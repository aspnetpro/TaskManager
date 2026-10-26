// Alertas (Cap. 14.5). Poucos e com ação clara: alerta que ninguém age vira ruído,
// e ruído ensina o time a ignorar o alerta crítico. Cada um tem entrada no
// runbook (docs/monitoring/runbook.md).
param name string
param location string
param tags object
param appInsightsId string
param databaseId string
param apiContainerAppId string
param webFqdn string
param alertEmail string

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'ag-${name}'
  location: 'global'
  tags: tags
  properties: {
    groupShortName: take(replace(name, '-', ''), 12)
    enabled: true
    emailReceivers: [{ name: 'oncall', emailAddress: alertEmail, useCommonAlertSchema: true }]
  }
}

// Sev 0: o usuário não consegue usar o sistema. Teste de disponibilidade de 3 regiões
// no /healthz do nginx (→ /health/live da Api): pega nginx fora, Api fora e Api sem
// réplica pronta (readiness falhando por banco/Redis = ingress devolve 503).
var webTestName = 'webtest-${name}-healthz'

resource webTest 'Microsoft.Insights/webtests@2022-06-15' = {
  name: webTestName
  location: location
  tags: union(tags, { 'hidden-link:${appInsightsId}': 'Resource' })
  kind: 'standard'
  properties: {
    SyntheticMonitorId: webTestName
    Name: webTestName
    Enabled: true
    Frequency: 300
    Timeout: 30
    Kind: 'standard'
    RetryEnabled: true
    Locations: [
      { Id: 'latam-br-gru-edge' }
      { Id: 'us-va-ash-azr' }
      { Id: 'emea-nl-ams-azr' }
    ]
    Request: { RequestUrl: 'https://${webFqdn}/healthz', HttpVerb: 'GET' }
    ValidationRules: { ExpectedHttpStatusCode: 200, SSLCheck: true, SSLCertRemainingLifetimeCheck: 7 }
  }
}

resource availabilityAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: 'alert-${name}-down'
  location: 'global'
  tags: tags
  properties: {
    description: 'TaskManager indisponível: /healthz falhando em 2+ regiões.'
    severity: 0
    enabled: true
    scopes: [webTest.id, appInsightsId]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.WebtestLocationAvailabilityCriteria'
      webTestId: webTest.id
      componentId: appInsightsId
      failedLocationCount: 2
    }
    actions: [{ actionGroupId: actionGroup.id }]
  }
}

// Alertas de log sobre o Application Insights (tabelas requests/customMetrics, vindas do OTel).
// A query só devolve linha quando a condição bate: o critério é "mais de 0 linhas".
// isnotempty(url): o exporter grava spans Consumer (processamento do Outbox) também em
// requests; erro/latência de HTTP não pode misturar com mensageria.
var logAlerts = [
  {
    key: 'errors'
    severity: 1
    description: 'Mais de 5% de respostas 5xx em 5 minutos.'
    window: 'PT5M'
    query: 'requests | where isnotempty(url) | summarize total = count(), failed = countif(toint(resultCode) >= 500) | where total >= 20 and failed * 100.0 / total > 5'
  }
  {
    key: 'latency'
    severity: 2
    description: 'p95 acima de 500 ms por 5 minutos (alvo do PRD: 200 ms).'
    window: 'PT5M'
    query: 'requests | where isnotempty(url) | summarize p95 = percentile(duration, 95), total = count() | where total >= 20 and p95 > 500'
  }
  {
    key: 'outbox-lag'
    severity: 2
    description: 'Outbox com mais de 60 s de atraso: notificações atrasadas, provável banco saturado ou handler falhando.'
    window: 'PT10M'
    query: 'customMetrics | where name == "taskmanager.outbox.lag" | summarize lag = max(valueMax) | where lag > 60'
  }
  {
    key: 'outbox-dead-letter'
    severity: 1
    description: 'Mensagem do Outbox desistiu após MaxAttempts: evento perdido até alguém reprocessar.'
    window: 'PT15M'
    query: 'customMetrics | where name == "taskmanager.outbox.messages" and tostring(customDimensions["outbox.outcome"]) == "dead_letter" | summarize total = sum(valueSum) | where total > 0'
  }
  {
    key: 'login-failures'
    severity: 3
    description: 'Mais de 50 logins inválidos em 5 minutos: credential stuffing? O rate limit por IP segura um IP, não uma botnet.'
    window: 'PT5M'
    query: 'requests | where name == "POST /api/v1/auth/login" and resultCode == "401" | summarize total = count() | where total > 50'
  }
]

resource queryAlerts 'Microsoft.Insights/scheduledQueryRules@2023-12-01' = [for alert in logAlerts: {
  name: 'alert-${name}-${alert.key}'
  location: location
  tags: tags
  properties: {
    description: alert.description
    severity: alert.severity
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT5M'
    windowSize: alert.window
    criteria: {
      allOf: [
        {
          query: alert.query
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: { numberOfEvaluationPeriods: 1, minFailingPeriodsToAlert: 1 }
        }
      ]
    }
    // Sem isto, a mesma condição dispara a cada 5 minutos até alguém resolver.
    muteActionsDuration: 'PT30M'
    autoMitigate: false
    actions: { actionGroups: [actionGroup.id] }
  }
}]

resource restartsAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: 'alert-${name}-restarts'
  location: 'global'
  tags: tags
  properties: {
    description: 'Api reiniciando (crash loop, OOM ou liveness falhando).'
    severity: 1
    enabled: true
    scopes: [apiContainerAppId]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'restarts'
          criterionType: 'StaticThresholdCriterion'
          metricName: 'RestartCount'
          timeAggregation: 'Total'
          operator: 'GreaterThan'
          threshold: 3
        }
      ]
    }
    actions: [{ actionGroupId: actionGroup.id }]
  }
}

resource dtuAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: 'alert-${name}-sql-dtu'
  location: 'global'
  tags: tags
  properties: {
    description: 'Azure SQL acima de 90% de DTU por 10 minutos: escalar o tier ou caçar a query.'
    severity: 2
    enabled: true
    scopes: [databaseId]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'dtu'
          criterionType: 'StaticThresholdCriterion'
          metricName: 'dtu_consumption_percent'
          timeAggregation: 'Average'
          operator: 'GreaterThan'
          threshold: 90
        }
      ]
    }
    actions: [{ actionGroupId: actionGroup.id }]
  }
}
