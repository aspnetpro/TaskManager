// Recursos compartilhados entre ambientes. Deploy uma vez, num resource group próprio
// (ex.: rg-taskmanager-shared). O ACR é um só: a MESMA imagem testada em staging é a
// que vai para produção — promover imagem, nunca rebuildar.
targetScope = 'resourceGroup'

@description('Nome do registry (5-50 caracteres alfanuméricos, único no Azure).')
param containerRegistryName string

param location string = resourceGroup().location

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  sku: { name: 'Standard' }
  properties: {
    // Pull só com identidade (AcrPull); push só pelo pipeline (service connection/OIDC).
    adminUserEnabled: false
    policies: {
      retentionPolicy: { status: 'disabled', days: 7 }
    }
  }
}

output loginServer string = registry.properties.loginServer
