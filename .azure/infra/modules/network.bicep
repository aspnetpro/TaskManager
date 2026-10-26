// VNet do ambiente: subnet do Container Apps + subnet de private endpoints, e as zonas
// de DNS privado que fazem o FQDN público de SQL, Key Vault e Redis resolver para IP privado.
param name string
param location string
param tags object

@description('Zonas privadas dos serviços com private endpoint.')
param privateDnsZoneNames array = [
  'privatelink${environment().suffixes.sqlServerHostname}'
  'privatelink.vaultcore.azure.net'
  'privatelink.redis.azure.net'
]

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    addressSpace: { addressPrefixes: ['10.40.0.0/16'] }
    subnets: [
      {
        // Workload profiles: /27 é o mínimo; /23 deixa espaço para escalar réplicas.
        name: 'snet-container-apps'
        properties: {
          addressPrefix: '10.40.0.0/23'
          delegations: [{ name: 'aca', properties: { serviceName: 'Microsoft.App/environments' } }]
        }
      }
      {
        name: 'snet-private-endpoints'
        properties: {
          addressPrefix: '10.40.2.0/24'
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
    ]
  }
}

resource zones 'Microsoft.Network/privateDnsZones@2024-06-01' = [for zone in privateDnsZoneNames: {
  name: zone
  location: 'global'
  tags: tags
}]

resource links 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = [for (zone, i) in privateDnsZoneNames: {
  parent: zones[i]
  name: '${name}-link'
  location: 'global'
  properties: {
    virtualNetwork: { id: vnet.id }
    registrationEnabled: false
  }
}]

output containerAppsSubnetId string = vnet.properties.subnets[0].id
output privateEndpointsSubnetId string = vnet.properties.subnets[1].id
output sqlDnsZoneId string = zones[0].id
output keyVaultDnsZoneId string = zones[1].id
output redisDnsZoneId string = zones[2].id
