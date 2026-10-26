// Azure SQL só com autenticação Entra: não existe senha de SQL para vazar.
// O admin Entra é a identidade do job de migrations (DDL); a Api entra como usuário
// comum (read/write), criado pelo próprio job (deploy/migrations/grant-api-access.sql).
param name string
param location string
param tags object
param skuName string
param publicNetworkAccess bool
param adminIdentityName string
param adminIdentityPrincipalId string

resource server 'Microsoft.Sql/servers@2023-08-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: publicNetworkAccess ? 'Enabled' : 'Disabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: adminIdentityName
      sid: adminIdentityPrincipalId
      principalType: 'Application'
      tenantId: subscription().tenantId
    }
  }
}

// Sem rede privada (staging): só serviços do Azure passam no firewall. Ninguém da internet.
resource allowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01' = if (publicNetworkAccess) {
  parent: server
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: server
  name: 'taskmanager'
  location: location
  tags: tags
  sku: { name: skuName }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    requestedBackupStorageRedundancy: 'Local'
  }
}

output id string = server.id
output fqdn string = server.properties.fullyQualifiedDomainName
output databaseName string = database.name
output databaseId string = database.id
