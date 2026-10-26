using '../main.bicep'

// Produção: rede privada (nada de dados com endpoint público), 2 réplicas mínimas
// (uma pode cair no deploy ou numa falha de zona sem derrubar o serviço).
param environmentName = 'production'
param containerRegistryName = readEnvironmentVariable('ACR_NAME', 'acrtaskmanager')
param containerRegistryResourceGroup = readEnvironmentVariable('ACR_RESOURCE_GROUP', 'rg-taskmanager-shared')
param enablePrivateNetworking = true
param sqlSkuName = 'S2'
param redisSkuName = 'Balanced_B1'
param logRetentionInDays = 90
param apiMinReplicas = 2
param apiMaxReplicas = 10
param webMinReplicas = 2
param webMaxReplicas = 5
param alertEmail = readEnvironmentVariable('ALERT_EMAIL', 'oncall@example.com')
