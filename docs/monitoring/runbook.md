# Runbook

Um alerta, uma entrada. Cada uma diz o que olhar primeiro e o prompt para o Claude Code
quando a causa não é óbvia. Alertas definidos em `.azure/infra/modules/alerts.bicep`;
queries em `dashboards.kql`.

Antes de qualquer coisa: **teve deploy nos últimos 30 minutos?**
`az containerapp revision list -g <rg> -n ca-taskmanager-<env>-api -o table`.
Se teve e o alerta começou junto, o rollback (docs/deploy.md) vem antes da investigação.

## Sev 0 — `down`: /healthz falhando em 2+ regiões

1. `curl -i https://<web>/healthz`. 502/503 = nginx de pé, Api não. Timeout = web fora.
2. Réplicas da Api: `az containerapp replica list -g <rg> -n ca-taskmanager-<env>-api -o table`.
3. Zero réplicas prontas: o readiness (`/health/ready`) checa SQL e Redis. Logs do container:
   `az containerapp logs show -g <rg> -n ca-taskmanager-<env>-api --tail 100`.
4. Readiness falhando por SQL/Redis: status do serviço no portal (Resource health).

## Sev 1 — `errors`: mais de 5% de 5xx em 5 minutos

1. Painel 2 (endpoints): é um endpoint ou todos? Todos = dependência (SQL/Redis/Key Vault).
2. `exceptions | where timestamp > ago(30m) | summarize count() by type, outerMessage`.
3. Abra um trace com erro (TraceId no log): o span vermelho mostra onde quebrou.

```
> Estamos com 5xx em <endpoint> desde <hora>. Seguem a exception e o trace
  (spans com duração). Correlacione com o código em @src/ e diga a causa raiz
  mais provável, como confirmar e a correção. Não altere código ainda.
```

## Sev 1 — `outbox-dead-letter`: mensagem desistiu após MaxAttempts

Um integration event não foi entregue (ex.: notificação de atribuição perdida).

1. `traces | where message has "gave up after"`: tipo e id da mensagem, com a exception.
2. No banco: `SELECT * FROM <schema>.OutboxMessages WHERE ProcessedAt IS NULL AND Attempts >= 5`.
3. Corrigida a causa, reprocessar: `UPDATE <schema>.OutboxMessages SET Attempts = 0, Error = NULL WHERE Id = '<id>'`.
   É seguro: os handlers são idempotentes por `EventId`.

## Sev 1 — `restarts`: Api reiniciando

1. `az containerapp logs show ... --type system`: OOMKilled? Liveness falhando?
2. OOM: memória subindo no gráfico da plataforma ao longo de horas = vazamento; pico súbito = request gigante.
3. Liveness (`/health/live`) não toca dependência. Se falha, o processo travou: thread pool
   esgotado (sync-over-async) é o suspeito número um.

## Sev 2 — `latency`: p95 acima de 500 ms por 5 minutos

1. Painel 2: endpoint mais lento. Painel 4: o SQL acompanhou?
2. Trace de uma request lenta: o tempo está no SQL, no cache, ou no handler?
3. SQL lento com DTU alto = query/índice. SQL lento com DTU baixo = lock (veja `sys.dm_exec_requests`).

```
> O p95 de <endpoint> subiu de <x> para <y> ms às <hora>. O trace mostra <z> ms
  no span <span>. Analise a query gerada pelo EF Core para esse caso de uso em
  @src/Modules/... e proponha índice ou reescrita. Mostre o plano de execução
  esperado antes e depois.
```

## Sev 2 — `outbox-lag`: Outbox com mais de 60 s de atraso

Notificações chegando atrasadas. Causas comuns: banco saturado (o polling compete com o
tráfego), handler lento ou falhando em loop (veja `taskmanager.outbox.messages` por `outcome`).
Lag alto **e** muitos `failed` = handler quebrado. Lag alto **e** só `processed` = vazão:
aumente `Outbox:BatchSize` ou reduza `Outbox:PollingInterval`.

## Sev 2 — `sql-dtu`: DTU acima de 90% por 10 minutos

1. Query Performance Insight (portal do Azure SQL): top queries por CPU/IO.
2. Uma query dominante: índice. Tudo subiu junto: tráfego, então escale o tier (S2 → S3).

## Sev 3 — `login-failures`: mais de 50 logins inválidos em 5 minutos

O rate limit por IP (10/min) segura um atacante, não uma botnet.

1. `requests | where name == "POST /api/v1/auth/login" and resultCode == "401" | summarize count() by client_IP`.
2. Muitos IPs, poucos e-mails = credential stuffing. Considere bloquear no WAF/Front Door
   e avise os donos das contas visadas.
3. Pico de 401 logo após um deploy pode ser bug no login, não ataque: teste com um usuário real.

## Incidente: usando o Claude Code

Com o incidente aberto, velocidade importa mais que elegância. Cole no Claude Code a query
KQL e o resultado, o trace do request ruim e o diff do último deploy
(`git diff <sha-anterior> <sha-atual> --stat`). Peça **causa raiz e como confirmar**, não a
correção direto: o modelo erra com confiança, e a confirmação é sua.
