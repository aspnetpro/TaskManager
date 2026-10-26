# Resultados dos testes de carga

Scripts em `tests/load/` (k6 2.3.0). Ambiente: **notebook de desenvolvimento**, Windows 11 +
Docker Desktop (WSL2), tudo na mesma máquina: SQL Server 2022, Redis, Api, nginx e o próprio k6.
Rate limit elevado para o teste (`AUTH_PERMIT_LIMIT`/`GLOBAL_PERMIT_LIMIT=100000`).
Data: 2026-10-26.

Número de notebook não é número de produção. Servem para **comparar execuções** (antes e
depois de uma mudança) e para achar gargalo de arquitetura, como os dois abaixo.

## Resumo

| Cenário | VUs | Req/s | p95 | p99 | Erros | Thresholds |
| --- | --- | --- | --- | --- | --- | --- |
| Smoke | 1 | 3 | 52 ms | — | 0% | ✅ |
| Load | 50 | 125 | **156 ms** | 561 ms | 0% | ❌ p99 < 500 ms; ❌ `POST /tasks` p95 < 300 ms (377 ms) |
| Stress (antes da correção do nginx) | 200 | 306 | 537 ms | — | **0,73%** | ✅ (mas os erros eram bug) |
| Stress (depois) | 200 | 281 | 576 ms | — | **0%** | ✅ |

O requisito do PRD (p95 < 200 ms com a carga esperada) foi atendido no Load: 156 ms.

## Achado 1: o stress test achou um bug no nginx (corrigido)

Com 200 VUs, 0,73% das requests falharam, mas a Api só respondeu 200/201. O log do nginx mostrava:

```
connect() to 172.18.0.5:8080 failed (99: Address not available) while connecting to upstream
```

**Esgotamento de portas efêmeras.** O `proxy_pass` com variável (usado para resolver o DNS em
runtime) não reaproveita conexão: um TCP novo por request. A ~300 req/s as portas acabam em
TIME_WAIT. Correção em `frontend/nginx/default.conf.template`: bloco `upstream` com `resolve`
(nginx ≥ 1.27.3), `keepalive 32` e HTTP/1.1 até a Api. Depois: 0 erro com a mesma carga.

Em produção o mesmo bug apareceria como "502 aleatório em horário de pico", com a Api saudável
e o dashboard da Api limpo. Sem teste de carga passando pelo proxy, quem acharia seria o cliente.

## Achado 2: a cauda do `POST /tasks` é fsync do log do SQL Server, não a Api

Os traces das requests mais lentas (Aspire Dashboard) mostram o tempo todo dentro de um único span:

```
POST /api/v1/tasks            1728 ms
└─ CreateTaskCommand          1727 ms
   └─ SQL INSERT              1726 ms
```

Wait stats do SQL Server durante o teste:

| Wait | Total (ms) | Máx (ms) |
| --- | --- | --- |
| `PREEMPTIVE_OS_FLUSHFILEBUFFERS` | 606 745 | 1 638 |
| `WRITELOG` | 337 454 | 708 |
| `LCK_M_U` + `LCK_M_S` | ~6 000 | 1 519 |

O commit espera o flush do log no disco virtual do Docker Desktop. Lock é desprezível. É limite
do ambiente, não da aplicação: o threshold do `POST /tasks` (p95 < 300 ms) **continua** sendo
o alvo, e a validação dele é contra staging (Azure SQL), não contra o notebook.

## Ponto de degradação (stress)

Até 200 VUs (~280 req/s), zero erro. O p95 sobe de 156 ms (50 VUs) para 576 ms (200 VUs),
puxado pelas escritas (achado 2). Não houve ponto de quebra dentro do teste: o limite desta
máquina está acima de 200 VUs.

## Próxima execução

Rodar `load.js` contra **staging** (`BASE_URL=https://<web-staging>/api/v1`, com os limites de
rate limit do staging elevados durante a janela do teste) e registrar aqui. É o número que vale
para o PRD.
