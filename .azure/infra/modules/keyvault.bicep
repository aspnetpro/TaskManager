// Key Vault com RBAC (sem access policies). A Api lê os segredos pela identidade gerenciada
// via AddAzureKeyVault: "Jwt--SigningKey" vira Jwt:SigningKey, "ConnectionStrings--Redis"
// vira ConnectionStrings:Redis. Nenhum segredo passa pelo pipeline nem pela config do app.
param name string
param location string
param tags object
param enablePurgeProtection bool
param publicNetworkAccess bool
param readerPrincipalId string

@secure()
@description('Só no primeiro deploy (scripts/ops/deploy-infra.sh --bootstrap). Vazio = não mexe no segredo existente.')
param jwtSigningKey string = ''

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    // Purge protection não pode ser desligada depois: só em produção.
    enablePurgeProtection: enablePurgeProtection ? true : null
    publicNetworkAccess: publicNetworkAccess ? 'Enabled' : 'Disabled'
    networkAcls: {
      defaultAction: publicNetworkAccess ? 'Allow' : 'Deny'
      bypass: 'AzureServices'
    }
  }
}

resource jwt 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(jwtSigningKey)) {
  parent: vault
  name: 'Jwt--SigningKey'
  properties: { value: jwtSigningKey }
}

// Key Vault Secrets User: lê valor de segredo, nada mais.
var secretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource readerRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: vault
  name: guid(vault.id, readerPrincipalId, secretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', secretsUserRoleId)
    principalId: readerPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output id string = vault.id
output name string = vault.name
output uri string = vault.properties.vaultUri
