# Deploy

Como o código sai do commit e chega ao Azure Container Apps. Infra em `.azure/infra/`,
pipelines em `.azure/pipelines/` (Azure DevOps) e `.github/workflows/` (GitHub Actions).
A lógica de deploy mora em `scripts/ci/`, e os dois pipelines chamam os mesmos scripts.

## Arquitetura

```
Internet ──HTTPS──▶ ca-…-web (nginx, ingress externo)
                      ├── /            SPA
                      ├── /healthz  ─▶ Api /health/live (teste de disponibilidade)
                      └── /api/*   ─▶ ca-…-api (ingress INTERNO, HTTPS)
                                         ├── Azure SQL (Entra only)
                                         ├── Azure Managed Redis
                                         ├── Key Vault (Jwt--SigningKey, ConnectionStrings--Redis)
                                         └── App Insights (OpenTelemetry)
caj-…-migrate (Container Apps Job) ──▶ Azure SQL (admin Entra)
```

| Identidade | Pode | Não pode |
| --- | --- | --- |
| `id-…-api` | AcrPull, ler segredos do Key Vault, ler/escrever dados no SQL | DDL, criar usuário |
| `id-…-web` | AcrPull | qualquer outra coisa |
| `id-…-migrator` | AcrPull, admin Entra do SQL (DDL, criar usuário da Api) | Key Vault |

Não existe senha de SQL, chave de ACR nem segredo no pipeline. O pipeline entra no Azure por
OIDC (workload identity federation) e só troca imagens e dispara o job.

## Primeiro deploy de um ambiente

```bash
# 1. Uma vez por assinatura: ACR compartilhado (a mesma imagem vai de staging para produção)
az group create -n rg-taskmanager-shared -l brazilsouth
az deployment group create -g rg-taskmanager-shared -f .azure/infra/shared.bicep \
  -p containerRegistryName=acrtaskmanager

# 2. Por ambiente
az group create -n rg-taskmanager-staging -l brazilsouth
export ACR_NAME=acrtaskmanager ACR_RESOURCE_GROUP=rg-taskmanager-shared ALERT_EMAIL=oncall@suaempresa.com
scripts/ops/deploy-infra.sh staging rg-taskmanager-staging --bootstrap
```

`--bootstrap` gera a chave JWT e grava no Key Vault. **Só no primeiro deploy**: rodar de novo
rotaciona a chave e invalida todas as sessões. Depois, mudanças de infra são
`scripts/ops/deploy-infra.sh staging rg-taskmanager-staging`. O script lê as imagens em
execução e repassa ao Bicep, então atualizar infra não volta a Api para o placeholder.

No primeiro deploy os Container Apps sobem com uma imagem placeholder. A primeira execução
do pipeline troca pelas imagens reais e roda as migrations.

### Service connection / OIDC

- **Azure DevOps**: service connection ARM `sc-taskmanager` com *workload identity federation*.
  Environments `taskmanager-staging` e `taskmanager-production`; no de produção,
  *Approvals and checks → Approvals* com os tech leads.
- **GitHub Actions**: app registration com federated credentials para `repo:<org>/<repo>:environment:staging`,
  `…:environment:production` e `…:ref:refs/heads/main`. Variáveis do repositório:
  `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `ACR_LOGIN_SERVER`.
  Environment `production` com *required reviewers*.
- Permissões da identidade do pipeline: `AcrPush` no ACR, `Contributor` nos resource groups dos ambientes.

## O que o pipeline faz

| Stage | Quando | O quê |
| --- | --- | --- |
| Build & Test | todo push/PR | build Release (warnings = erro), testes com cobertura (75% global, 90% Tasks.Domain), scripts SQL das migrations como artifact, frontend (lint, typecheck, testes, build) |
| Security | todo push/PR, em paralelo | NuGet vulnerável (inclusive transitivo), `npm audit` de produção, gitleaks no histórico |
| Images | após os dois | build de `api`, `web` e `migrations`; Trivy (CRITICAL/HIGH corrigíveis barram); push no ACR só na `main`, tag = SHA do commit |
| Deploy Staging | `main`, automático | `scripts/ci/deploy.sh staging` |
| Deploy Production | após staging + aprovação | `scripts/ci/deploy.sh production` (mesma imagem) |

Tag é sempre o SHA, nunca `latest`: dá para saber exatamente o que está rodando, e rollback é
apontar para a tag anterior.

## `scripts/ci/deploy.sh`

1. Guarda as imagens atuais da Api e do web (alvo de rollback).
2. Atualiza a imagem do job de migrations e executa. **Falhou: para aqui.** Nada foi trocado.
3. Troca a imagem da Api e espera a revision ficar `Running`. Em modo `Single`, a revision nova
   só recebe tráfego quando o readiness (`/health/ready`: SQL + Redis) passa.
4. Mesmo para o web.
5. Smoke test (`scripts/ci/smoke-test.sh`), sem credencial e sem criar dado:
   SPA 200, `/healthz` 200, login inválido 401 (Api + banco respondendo), rota protegida 401.
6. Falha nos passos 3 a 5: Api e web voltam para as imagens anteriores.

## Migrations

Rodam no **Container Apps Job** `caj-…-migrate`, dentro do ambiente (e da VNet em produção).
A imagem (`docker build --target migrations`) traz:

- uma EF Core bundle por módulo (`efbundle-auth`, `-tasks`, `-notifications`): idempotentes,
  e o EF Core 10 segura um lock no banco, então duas execuções simultâneas não se atropelam;
- `tools/TaskManager.DbAccess`: garante o usuário da Api no banco (`db_datareader` +
  `db_datawriter`), criado por SID (client id), sem precisar de Directory Readers no SQL.

O aprovador de produção revisa os scripts idempotentes do artifact `migrations-sql`
(gerados no Build) **antes** de aprovar.

### Regra: toda migration é backward-compatible

Migration não tem rollback automático. Quando o smoke test falha, a Api **anterior** volta a
rodar sobre o banco **já migrado**. Então a versão N-1 precisa funcionar com o schema N:

| Mudança | Como |
| --- | --- |
| Adicionar coluna | nullable ou com default. Um deploy |
| Renomear coluna | expand-contract: (1) adiciona a nova e escreve nas duas; (2) migra os dados e passa a ler a nova; (3) remove a antiga. Três deploys |
| Remover coluna | só depois que nenhuma versão em produção a lê (o deploy anterior já parou de usar) |
| Mudar tipo | nova coluna + expand-contract |
| Índice em tabela grande | `ONLINE = ON` em SQL script manual, fora da migration do EF |

Precisa desfazer uma migration? Gere o script reverso, revise e aplique manualmente:
`dotnet ef migrations script <atual> <alvo> --context <Modulo>DbContext ...`.

## Rollback manual

```bash
# Versões disponíveis
az containerapp revision list -g rg-taskmanager-production -n ca-taskmanager-production-api -o table
# Voltar a Api para uma tag (SHA) anterior
az containerapp update -g rg-taskmanager-production -n ca-taskmanager-production-api \
  --image acrtaskmanager.azurecr.io/taskmanager-api:<sha-anterior>
```

| Cenário | Quem detecta | O que acontece | Tempo |
| --- | --- | --- | --- |
| Readiness falha na revision nova | Container Apps | tráfego fica na revision anterior | imediato |
| Smoke test falha | pipeline | `deploy.sh` volta Api e web | ~2 min |
| Bug percebido depois | time / alerta | rollback manual acima | ~2 min |
| Migration falha | pipeline | nada é trocado; investigar o log do job | — |

## Ambientes

| | Development | Staging | Production |
| --- | --- | --- | --- |
| Banco | SQL Server (compose) | Azure SQL S1, acesso público só para serviços Azure | Azure SQL S2, private endpoint |
| Cache | Redis (compose) | Azure Managed Redis Balanced_B0 | Azure Managed Redis Balanced_B1, private endpoint |
| Segredos | user-secrets / `.env` | Key Vault | Key Vault (private endpoint, purge protection) |
| Telemetria | Aspire Dashboard (OTLP) | Application Insights | Application Insights |
| Réplicas (Api) | 1 | 1–3 | 2–10 |
| Deploy | `docker compose` | automático após merge | aprovação manual |

Não existe `appsettings.Staging.json` nem `appsettings.Production.json`: o que muda por ambiente
vem de variável de ambiente (Bicep) e do Key Vault. Um lugar só para procurar.
