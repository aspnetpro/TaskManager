# Testes de carga (k6)

Três cenários sobre a mesma jornada de usuário (`lib/api.js`): cria tarefa, lista,
abre o detalhe e, em 30% das iterações, leva a tarefa até `Done`.

| Script | VUs | Duração | Pergunta que responde | Thresholds |
| --- | --- | --- | --- | --- |
| `smoke.js` | 1 | 30 s | A jornada funciona? | p95 < 500 ms, erros < 1% |
| `load.js` | 50 | 5 min (rampa 1 + 3 + 1) | Aguenta a carga esperada? | p95 < 200 ms, erros < 0,1% |
| `stress.js` | 200 | 5 min | Onde começa a degradar? | aborta se p95 > 3 s |

Cada VU registra o próprio usuário e workspace no `setup()`: o filtro de tenant e o
cache trabalham como em produção, e nenhum VU disputa a mesma tarefa com outro.

## Rodando contra o compose

Todos os VUs saem do mesmo IP. Com os limites padrão (10 logins/min por IP,
100 requests/min por usuário), o teste mede o rate limiter, não a Api. Suba a stack
com limites altos — **só para o teste**:

```bash
AUTH_PERMIT_LIMIT=100000 GLOBAL_PERMIT_LIMIT=100000 docker compose --profile app up -d --build
```

PowerShell: `$env:AUTH_PERMIT_LIMIT=100000; $env:GLOBAL_PERMIT_LIMIT=100000; docker compose --profile app up -d --build`

k6 via Docker (sem instalar nada), passando pelo nginx como em produção:

```bash
docker run --rm --network taskmanager_default -v "$PWD/tests/load:/scripts" \
  -e BASE_URL=http://taskmanager-web:8080/api/v1 grafana/k6:2.3.0 run /scripts/smoke.js
```

Com k6 instalado: `k6 run tests/load/smoke.js` (padrão: `http://localhost:3000/api/v1`).

Depois, volte os limites ao normal: `docker compose --profile app up -d`.

## Lendo o resultado

- `http_req_duration` p95/p99: comparar com o alvo do PRD (p95 < 200 ms).
- `http_req_duration{name:...}`: o endpoint culpado quando o global estoura.
- Durante o teste, o Aspire Dashboard (http://localhost:18888) mostra os traces lentos
  e o `taskmanager.outbox.lag` — Outbox atrasando é o primeiro sintoma de banco saturado.
- Números de máquina de dev não são números de produção. O que vale aqui é a
  **tendência entre execuções**: rode antes e depois de uma mudança suspeita.

Resultados de referência: [`docs/load-test-results.md`](../../docs/load-test-results.md).
