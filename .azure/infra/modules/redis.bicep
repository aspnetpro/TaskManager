// Azure Managed Redis. O Azure Cache for Redis (Basic/Standard/Premium) não aceita
// instâncias novas desde 01/10/2026 e é aposentado em 30/09/2028.
// A connection string vai direto para o Key Vault: nunca vira output do deployment.
param name string
param location string
param tags object
param skuName string
param publicNetworkAccess bool
param keyVaultName string

resource cluster 'Microsoft.Cache/redisEnterprise@2025-07-01' = {
  name: name
  location: location
  tags: tags
  sku: { name: skuName }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: publicNetworkAccess ? 'Enabled' : 'Disabled'
  }
}

resource database 'Microsoft.Cache/redisEnterprise/databases@2025-07-01' = {
  parent: cluster
  name: 'default'
  properties: {
    clientProtocol: 'Encrypted'
    port: 10000
    // Endpoint único (proxy): o StackExchange.Redis não precisa saber de cluster.
    clusteringPolicy: 'EnterpriseCluster'
    evictionPolicy: 'VolatileLRU'
    accessKeysAuthentication: 'Enabled'
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'ConnectionStrings--Redis'
  properties: {
    value: '${cluster.properties.hostName}:10000,password=${database.listKeys().primaryKey},ssl=True,abortConnect=False'
  }
}

output id string = cluster.id
