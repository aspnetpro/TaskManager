// Container Apps: ambiente + web (nginx, ingress externo) + api (ingress interno) + job de migrations.
// Só o nginx é público. A Api só é alcançável de dentro do ambiente — mesmo arranjo do
// docker-compose: o navegador fala com uma origem só, e o cookie de refresh funciona sem CORS.
param name string
param location string
param tags object
param environmentName string
param logAnalyticsWorkspaceName string
param infrastructureSubnetId string = ''

param registryServer string
param apiImage string
param webImage string
param migrationsImage string

param apiIdentityId string
param apiIdentityClientId string
param apiIdentityName string
param webIdentityId string
param migratorIdentityId string
param migratorIdentityClientId string

param keyVaultUri string
param sqlServerFqdn string
param sqlDatabaseName string
@secure()
param appInsightsConnectionString string

param apiMinReplicas int
param apiMaxReplicas int
param webMinReplicas int
param webMaxReplicas int

var aspNetEnvironment = environmentName == 'production' ? 'Production' : 'Staging'
var sqlBase = 'Server=tcp:${sqlServerFqdn},1433;Database=${sqlDatabaseName};Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Managed Identity'

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: 'cae-${name}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: workspace.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [{ name: 'Consumption', workloadProfileType: 'Consumption' }]
    vnetConfiguration: empty(infrastructureSubnetId) ? null : {
      infrastructureSubnetId: infrastructureSubnetId
      internal: false
    }
  }
}

resource api 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'ca-${name}-api'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${apiIdentityId}': {} }
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      // Single: uma revision ativa. A nova só recebe tráfego quando o readiness passa.
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      registries: [{ server: registryServer, identity: apiIdentityId }]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: aspNetEnvironment }
            // DefaultAzureCredential (Key Vault) e SqlClient usam esta identidade.
            { name: 'AZURE_CLIENT_ID', value: apiIdentityClientId }
            { name: 'KeyVault__VaultUri', value: keyVaultUri }
            { name: 'ConnectionStrings__Database', value: '${sqlBase};User Id=${apiIdentityClientId}' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
          ]
          probes: [
            // Startup generoso: primeira conexão com Key Vault + SQL depois de um cold start.
            { type: 'Startup', httpGet: { path: '/health/live', port: 8080 }, periodSeconds: 5, failureThreshold: 24 }
            { type: 'Liveness', httpGet: { path: '/health/live', port: 8080 }, periodSeconds: 10, failureThreshold: 3 }
            { type: 'Readiness', httpGet: { path: '/health/ready', port: 8080 }, periodSeconds: 10, failureThreshold: 3 }
          ]
        }
      ]
      scale: {
        minReplicas: apiMinReplicas
        maxReplicas: apiMaxReplicas
        rules: [{ name: 'http', http: { metadata: { concurrentRequests: '100' } } }]
      }
    }
  }
}

resource web 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'ca-${name}-web'
  location: location
  tags: tags
  // Identidade própria só para o pull da imagem: o nginx não lê segredo nenhum.
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${webIdentityId}': {} }
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      registries: [{ server: registryServer, identity: webIdentityId }]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: webImage
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            // FQDN interno da Api (<app>.internal.<domínio do ambiente>), em HTTPS.
            { name: 'API_HOST', value: api.properties.configuration.ingress.fqdn }
            { name: 'API_PORT', value: '443' }
            { name: 'API_SCHEME', value: 'https' }
          ]
          probes: [
            { type: 'Liveness', httpGet: { path: '/', port: 8080 }, periodSeconds: 10 }
            { type: 'Readiness', httpGet: { path: '/', port: 8080 }, periodSeconds: 10 }
          ]
        }
      ]
      scale: {
        minReplicas: webMinReplicas
        maxReplicas: webMaxReplicas
        rules: [{ name: 'http', http: { metadata: { concurrentRequests: '200' } } }]
      }
    }
  }
}

// Disparado pelo pipeline antes de trocar a imagem da Api (scripts/ci/run-migrations.sh).
resource migrations 'Microsoft.App/jobs@2025-01-01' = {
  name: 'caj-${name}-migrate'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${migratorIdentityId}': {} }
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 900
      // Migration que falhou não tenta de novo sozinha: alguém precisa olhar.
      replicaRetryLimit: 0
      manualTriggerConfig: { parallelism: 1, replicaCompletionCount: 1 }
      registries: [{ server: registryServer, identity: migratorIdentityId }]
    }
    template: {
      containers: [
        {
          name: 'migrate'
          image: migrationsImage
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'AZURE_CLIENT_ID', value: migratorIdentityClientId }
            { name: 'ConnectionStrings__Database', value: '${sqlBase};User Id=${migratorIdentityClientId}' }
            { name: 'SQL_SERVER', value: sqlServerFqdn }
            { name: 'SQL_DATABASE', value: sqlDatabaseName }
            { name: 'API_IDENTITY_NAME', value: apiIdentityName }
            { name: 'API_IDENTITY_CLIENT_ID', value: apiIdentityClientId }
          ]
        }
      ]
    }
  }
}

output apiName string = api.name
output apiId string = api.id
output webName string = web.name
output webFqdn string = web.properties.configuration.ingress.fqdn
output migrationsJobName string = migrations.name
