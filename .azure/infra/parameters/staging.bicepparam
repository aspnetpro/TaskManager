using '../main.bicep'

// Staging: barato, sem rede privada (SQL/Key Vault/Redis públicos, protegidos por Entra,
// firewall "só Azure" e TLS). Escala a zero no web; a Api fica com 1 réplica quente.
param environmentName = 'staging'
param containerRegistryName = readEnvironmentVariable('ACR_NAME', 'acrtaskmanager')
param containerRegistryResourceGroup = readEnvironmentVariable('ACR_RESOURCE_GROUP', 'rg-taskmanager-shared')
param enablePrivateNetworking = false
param sqlSkuName = 'S1'
param redisSkuName = 'Balanced_B0'
param logRetentionInDays = 30
param apiMinReplicas = 1
param apiMaxReplicas = 3
param webMinReplicas = 0
param webMaxReplicas = 2
param alertEmail = readEnvironmentVariable('ALERT_EMAIL', 'oncall@example.com')
