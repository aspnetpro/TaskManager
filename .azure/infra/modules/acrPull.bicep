// AcrPull no registry compartilhado (outro resource group): deploy com scope do RG do ACR.
param registryName string
param principalIds array

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: registryName
}

var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource pull 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in principalIds: {
  scope: registry
  name: guid(registry.id, principalId, acrPullRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}]
