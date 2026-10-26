// Infraestrutura de um ambiente do TaskManager (staging ou production).
// Deploy: scripts/ops/deploy-infra.sh <ambiente> (preserva as imagens em execução).
targetScope = 'resourceGroup'

@allowed(['staging', 'production'])
param environmentName string

param location string = resourceGroup().location

@description('ACR compartilhado (shared.bicep), em outro resource group.')
param containerRegistryName string
param containerRegistryResourceGroup string

@description('Imagens atuais. O deploy-infra.sh passa as que estão rodando; no primeiro deploy, o placeholder.')
param apiImage string = 'mcr.microsoft.com/k8se/quickstart:latest'
param webImage string = 'mcr.microsoft.com/k8se/quickstart:latest'
param migrationsImage string = 'mcr.microsoft.com/k8se/quickstart:latest'

@description('VNet + private endpoints para SQL, Key Vault e Redis (sem acesso público).')
param enablePrivateNetworking bool

param sqlSkuName string
param redisSkuName string
param logRetentionInDays int
param apiMinReplicas int
param apiMaxReplicas int
param webMinReplicas int
param webMaxReplicas int
param alertEmail string

@secure()
@description('Só no primeiro deploy (--bootstrap). Vazio = mantém o segredo existente.')
param jwtSigningKey string = ''

var name = 'taskmanager-${environmentName}'
// Nomes globais (Key Vault, SQL, Redis) precisam ser únicos no Azure inteiro.
var suffix = take(uniqueString(resourceGroup().id), 6)
var tags = { application: 'taskmanager', environment: environmentName }
var publicAccess = !enablePrivateNetworking

// Três identidades, cada uma com o mínimo: api (Key Vault + dados), web (só pull de imagem)
// e migrator (admin Entra do SQL, só usado pelo job de migrations).
resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${name}-api'
  location: location
  tags: tags
}

resource webIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${name}-web'
  location: location
  tags: tags
}

resource migratorIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${name}-migrator'
  location: location
  tags: tags
}

module acrPull 'modules/acrPull.bicep' = {
  name: 'acr-pull-${environmentName}'
  scope: resourceGroup(containerRegistryResourceGroup)
  params: {
    registryName: containerRegistryName
    principalIds: [apiIdentity.properties.principalId, webIdentity.properties.principalId, migratorIdentity.properties.principalId]
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: containerRegistryName
  scope: resourceGroup(containerRegistryResourceGroup)
}

module network 'modules/network.bicep' = if (enablePrivateNetworking) {
  name: 'network'
  params: { name: 'vnet-${name}', location: location, tags: tags }
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: { name: name, location: location, tags: tags, retentionInDays: logRetentionInDays }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    name: 'kv-tm-${environmentName == 'production' ? 'prd' : 'stg'}-${suffix}'
    location: location
    tags: tags
    enablePurgeProtection: environmentName == 'production'
    publicNetworkAccess: publicAccess
    readerPrincipalId: apiIdentity.properties.principalId
    jwtSigningKey: jwtSigningKey
  }
}

module database 'modules/database.bicep' = {
  name: 'database'
  params: {
    name: 'sql-${name}-${suffix}'
    location: location
    tags: tags
    skuName: sqlSkuName
    publicNetworkAccess: publicAccess
    adminIdentityName: migratorIdentity.name
    adminIdentityPrincipalId: migratorIdentity.properties.principalId
  }
}

module redis 'modules/redis.bicep' = {
  name: 'redis'
  params: {
    name: 'redis-${name}-${suffix}'
    location: location
    tags: tags
    skuName: redisSkuName
    publicNetworkAccess: publicAccess
    keyVaultName: keyVault.outputs.name
  }
}

module sqlEndpoint 'modules/privateEndpoint.bicep' = if (enablePrivateNetworking) {
  name: 'pe-sql'
  params: {
    name: 'pe-${name}-sql'
    location: location
    tags: tags
    subnetId: network!.outputs.privateEndpointsSubnetId
    targetResourceId: database.outputs.id
    groupId: 'sqlServer'
    privateDnsZoneId: network!.outputs.sqlDnsZoneId
  }
}

module keyVaultEndpoint 'modules/privateEndpoint.bicep' = if (enablePrivateNetworking) {
  name: 'pe-keyvault'
  params: {
    name: 'pe-${name}-kv'
    location: location
    tags: tags
    subnetId: network!.outputs.privateEndpointsSubnetId
    targetResourceId: keyVault.outputs.id
    groupId: 'vault'
    privateDnsZoneId: network!.outputs.keyVaultDnsZoneId
  }
}

module redisEndpoint 'modules/privateEndpoint.bicep' = if (enablePrivateNetworking) {
  name: 'pe-redis'
  params: {
    name: 'pe-${name}-redis'
    location: location
    tags: tags
    subnetId: network!.outputs.privateEndpointsSubnetId
    targetResourceId: redis.outputs.id
    groupId: 'redisEnterprise'
    privateDnsZoneId: network!.outputs.redisDnsZoneId
  }
}

module apps 'modules/containerApps.bicep' = {
  name: 'container-apps'
  // Pull liberado e private endpoints prontos ANTES de o Container App subir a primeira réplica.
  dependsOn: [acrPull, sqlEndpoint, keyVaultEndpoint, redisEndpoint]
  params: {
    name: name
    location: location
    tags: tags
    environmentName: environmentName
    logAnalyticsWorkspaceName: monitoring.outputs.workspaceName
    infrastructureSubnetId: enablePrivateNetworking ? network!.outputs.containerAppsSubnetId : ''
    registryServer: registry.properties.loginServer
    apiImage: apiImage
    webImage: webImage
    migrationsImage: migrationsImage
    apiIdentityId: apiIdentity.id
    apiIdentityClientId: apiIdentity.properties.clientId
    apiIdentityName: apiIdentity.name
    webIdentityId: webIdentity.id
    migratorIdentityId: migratorIdentity.id
    migratorIdentityClientId: migratorIdentity.properties.clientId
    keyVaultUri: keyVault.outputs.uri
    sqlServerFqdn: database.outputs.fqdn
    sqlDatabaseName: database.outputs.databaseName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    apiMinReplicas: apiMinReplicas
    apiMaxReplicas: apiMaxReplicas
    webMinReplicas: webMinReplicas
    webMaxReplicas: webMaxReplicas
  }
}

module alerts 'modules/alerts.bicep' = {
  name: 'alerts'
  params: {
    name: name
    location: location
    tags: tags
    appInsightsId: monitoring.outputs.appInsightsId
    databaseId: database.outputs.databaseId
    apiContainerAppId: apps.outputs.apiId
    webFqdn: apps.outputs.webFqdn
    alertEmail: alertEmail
  }
}

output apiContainerAppName string = apps.outputs.apiName
output webContainerAppName string = apps.outputs.webName
output migrationsJobName string = apps.outputs.migrationsJobName
output webUrl string = 'https://${apps.outputs.webFqdn}'
output keyVaultName string = keyVault.outputs.name
