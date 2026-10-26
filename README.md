# TaskManager

Projeto de referência do livro **Claude Code para Desenvolvedores .NET — Guia Definitivo**.
Modular Monolith em .NET 10 com Clean Architecture, Vertical Slices e multi-tenancy por workspace.

**Status: Fase 4 (completo)** — backend de produção (Caps. 7 e 8), frontend React (Cap. 9),
qualidade (Cap. 12), CI/CD (Cap. 13) e observabilidade/infra (Cap. 14):

- **Tasks**: US-001 a US-008 completas (Fase 1)
- **Auth**: registro, login, JWT próprio (15 min) + refresh token rotativo em cookie httpOnly (7 dias), detecção de reuso, membros do workspace
- **Notifications**: notificações in-app e email, alimentadas por integration events via **Outbox**
- Cache distribuído (**HybridCache + Redis**) com escopo de workspace, **rate limiting**, CORS, security headers, health checks, **OpenAPI + Scalar**, Key Vault opcional
- `docker-compose` com SQL Server, Redis, a Api e o frontend (nginx)
- **Frontend** (`frontend/`): React 19 + TypeScript + Vite, Board Kanban com drag and drop e
  optimistic update, lista paginada, detalhe em drawer com edição (`If-Match`), notificações,
  sessão por cookie httpOnly com refresh single-flight
- **Observabilidade**: Serilog (JSON fora de dev) + OpenTelemetry (traces, métricas de negócio e do
  Outbox, logs). Aspire Dashboard no dev local; Application Insights no Azure
- **CI/CD**: Azure DevOps (`.azure/pipelines/`) e GitHub Actions (`.github/workflows/`) chamando os
  mesmos scripts (`scripts/ci/`): testes com gate de cobertura, NuGet/npm audit, gitleaks, Trivy,
  deploy por SHA com migrations em Container Apps Job, smoke test e rollback. Zero secret no pipeline
- **Infra**: Bicep em `.azure/infra/` (Container Apps, Azure SQL Entra-only, Azure Managed Redis,
  Key Vault, identidades por componente, rede privada em produção, 8 alertas)
- **Testes de carga** k6 em `tests/load/` (resultados em `docs/load-test-results.md`)

## Pré-requisitos

- .NET SDK 10.0.100+
- Node.js 22.12+ (frontend; desenvolvido com Node 24)
- Docker Desktop (SQL Server, Redis e Aspire Dashboard locais, testes de integração)
- Deploy (opcional): Azure CLI 2.60+ com Bicep (`az bicep install`)

## Rodando localmente

```bash
# 0. Código
git clone https://github.com/aspnetpro/TaskManager.git
cd TaskManager

# 1. Dependências (SQL Server + Redis + Aspire Dashboard)
cp .env.example .env              # troque a senha do SQL
docker compose up -d

# 2. Secrets (ficam em user-secrets, fora do Git)
cd src/TaskManager.Api
dotnet user-secrets set "ConnectionStrings:Database" \
  "Server=localhost,1433;Database=TaskManager;User Id=sa;Password=<senha do .env>;TrustServerCertificate=true"
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"
# PowerShell: dotnet user-secrets set "Jwt:SigningKey" ([Convert]::ToBase64String((1..48 | % { Get-Random -Max 256 })))

# 3. Subir a Api (aplica migrations e cria o workspace demo em Development)
dotnet run --launch-profile http   # http://localhost:5000
```

Sem `Jwt:SigningKey` a Api não sobe: as options são validadas no startup, de propósito.

Documentação interativa (Development): `http://localhost:5000/scalar` — OpenAPI em `/openapi/v1.json`.

### Frontend

```bash
cd frontend
npm ci
npm run dev        # http://localhost:5173 — proxy de /api para http://localhost:5000
```

O proxy do Vite deixa SPA e Api na mesma origem: o cookie de refresh (`SameSite=Strict`,
`Path=/api/v1/auth`) funciona sem CORS. Api em outra porta: `API_PROXY_TARGET=http://localhost:8080 npm run dev`.

### Tudo em container

```bash
# no .env: JWT_SIGNING_KEY=<32+ caracteres aleatórios>
docker compose --profile app up -d --build    # SPA em http://localhost:3000, Api em http://localhost:8080
```

O nginx do frontend serve a SPA e faz proxy de `/api` para a Api — mesma origem, mesmo
comportamento de cookie da produção. Os containers rodam com filesystem somente leitura,
sem capabilities, e as portas só escutam em `127.0.0.1`.

### Telemetria local

Logs, traces e métricas aparecem no **Aspire Dashboard**: http://localhost:18888. A Api rodando
com `dotnet run` exporta para `localhost:4317` (`appsettings.Development.json`); em container,
para o serviço `aspire-dashboard` do compose. Cada request da SPA vira um trace:
`POST /api/v1/tasks` → `CreateTaskCommand` → `SQL INSERT`.

### Usuários demo

O seed de Development cria um usuário por papel no workspace demo, todos com a senha
`DemoWorkspace:Password` do `appsettings.Development.json` (`demo-password-123`, só dev):

| Papel | Email |
| --- | --- |
| Admin | `admin@demo.local` |
| TechLead | `techlead@demo.local` |
| Developer | `developer@demo.local` |
| Viewer | `viewer@demo.local` |

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"techlead@demo.local","password":"demo-password-123"}' | jq -r .accessToken)

curl -X POST http://localhost:5000/api/v1/tasks \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"title":"Minha primeira tarefa","priority":"High"}'
```

O refresh token nunca aparece no body: vai no cookie `tm_refresh` (httpOnly, Secure,
SameSite=Strict, `Path=/api/v1/auth`). Cada `POST /auth/refresh` troca o cookie; reapresentar
um token já trocado derruba a sessão inteira.

## Endpoints

| Método | Rota | O quê | Quem pode |
| --- | --- | --- | --- |
| POST | `/api/v1/auth/register` | Conta + primeiro workspace (como Admin) | anônimo |
| POST | `/api/v1/auth/login` | Access token (`workspaceId` opcional) + cookie de refresh | anônimo |
| POST | `/api/v1/auth/refresh` | Rotaciona o cookie, novo access token | cookie |
| POST | `/api/v1/auth/logout` | Revoga a sessão e apaga o cookie | cookie |
| GET | `/api/v1/workspace/members` | Membros do workspace do token | membros |
| POST | `/api/v1/workspace/members` | Adiciona conta existente por email | Admin |
| POST | `/api/v1/tasks` | US-001 criar | Admin, TechLead, Developer |
| GET | `/api/v1/tasks` | US-002 listar (`status`, `priority`, `assigneeId`, `search`, `overdue`, `includeDeleted`, `sortBy`, `sortDirection`, `page`, `pageSize`) | membros; `includeDeleted` só Admin |
| GET | `/api/v1/tasks/{id}` | US-003 detalhe + histórico (`ETag`, cacheado) | membros |
| PATCH | `/api/v1/tasks/{id}` | US-004 editar (exige `If-Match`) | reporter ou assignee |
| PATCH | `/api/v1/tasks/{id}/status` | US-005 transição | Admin, TechLead, Developer |
| PUT | `/api/v1/tasks/{id}/assignee` | US-006 atribuir | Admin/TechLead: qualquer membro; Developer: só a si |
| DELETE | `/api/v1/tasks/{id}` | US-007 soft delete | Admin |
| POST | `/api/v1/tasks/{id}/restore` | US-007 restaurar (até 30 dias) | Admin |
| GET | `/api/v1/notifications` | Notificações do usuário (`unreadOnly`, `page`, `pageSize`) | membros |
| POST | `/api/v1/notifications/{id}/read` | Marca como lida | destinatário |
| GET | `/health/live`, `/health/ready`, `/health` | Liveness, readiness (SQL + Redis), tudo | anônimo |
| GET | `/healthz` (nginx) | Proxy para `/health/live`: teste de disponibilidade e smoke test | anônimo |

Rate limiting: 100 req/min por usuário (ou IP, se anônimo); 10 req/min por IP nos endpoints de auth.
Excedeu: `429` com `Retry-After`. Atrás de proxy, o IP é o do cliente: `UseForwardedHeaders` lê o
`X-Forwarded-For` pulando só redes confiáveis (`ForwardedHeaders:KnownNetworks`).

## Testes

```bash
dotnet test                                   # tudo (Docker precisa estar rodando)
dotnet test --filter Category!=Integration    # só unitários + arquitetura
cd frontend && npm test                       # frontend (Vitest + Testing Library + MSW)
bash scripts/ci/coverage.sh                   # tudo + cobertura (gate: 75% global, 90% Tasks.Domain)
```

Hoje: 216 testes .NET (91% de cobertura de linhas; 98% no Tasks.Domain) e 49 de frontend.

Testes de carga (k6, via Docker): ver [`tests/load/README.md`](tests/load/README.md).

| Projeto | O que cobre |
| --- | --- |
| `TaskManager.Tasks.UnitTests` | Domínio (transições, soft delete, due date), handlers, behaviors de validação e de cache |
| `TaskManager.Auth.UnitTests` | Rotação e reuso de refresh token, login (timing, rehash), registro |
| `TaskManager.Notifications.UnitTests` | Consumidores dos integration events: destinatários, idempotência, retry |
| `TaskManager.Api.IntegrationTests` | Api inteira contra SQL Server real (Testcontainers): tenant isolation, ETag, JWT real, cookie de refresh, Outbox ponta a ponta, cache, rate limit, health |
| `TaskManager.ArchitectureTests` | Regras de dependência entre camadas e módulos, convenções de naming |
| `tests/load` (k6) | Smoke, load (50 VUs, p95 < 200 ms) e stress (200 VUs) pelo nginx |
| `frontend` (`*.test.ts[x]`) | Cliente HTTP (refresh single-flight, ProblemDetails), regras espelhadas (transições, permissões), optimistic update + rollback, fluxos de login/sessão, board e drawer contra uma Api fake (MSW) |

## Deploy

Azure Container Apps, com infra em Bicep e pipeline em Azure DevOps ou GitHub Actions.
Arquitetura, identidades, primeiro deploy, migrations e rollback: [`docs/deploy.md`](docs/deploy.md).
Alertas e o que fazer quando disparam: [`docs/monitoring/runbook.md`](docs/monitoring/runbook.md).

```bash
docker build --target runtime    -t taskmanager-api .          # Api (chiseled, não-root)
docker build --target migrations -t taskmanager-migrations .   # EF bundles + DbAccess (job)
docker build -t taskmanager-web frontend                       # nginx sem root
az bicep build --file .azure/infra/main.bicep                  # valida a infra
```

## Estrutura

```
src/
├── TaskManager.Api/                 # Composition root: endpoints, segurança, hosting
├── Shared/                          # Shared Kernel (Domain, Application, Infrastructure: Outbox, cache)
└── Modules/
    ├── Tasks/          (Domain, Application, Infrastructure, Contracts)
    ├── Auth/           (Domain, Application, Infrastructure, Contracts)
    └── Notifications/  (Domain, Application, Infrastructure)
frontend/
├── src/api/                         # fetch + refresh single-flight, endpoints tipados
├── src/hooks/                       # TanStack Query: queries, mutations, optimistic update
├── src/components/                  # ui/, board/, tasks/, layout/, auth/
├── src/pages/                       # rotas (lazy)
├── nginx/                           # config do container (SPA + proxy /api + headers)
└── Dockerfile
tools/TaskManager.DbAccess/          # Grant do usuário da Api no Azure SQL (roda no job de migrations)
deploy/migrations/migrate.sh         # Entrypoint do job de migrations
.azure/infra/                        # Bicep: shared (ACR), main (ambiente), modules/, parameters/
.azure/pipelines/                    # Azure DevOps
.github/workflows/                   # GitHub Actions (ci, cd, deploy)
scripts/ci/                          # Lógica dos pipelines: cobertura, audit, deploy, smoke test
scripts/ops/deploy-infra.sh          # Deploy da infra preservando as imagens em execução
docs/                                # deploy, monitoring (KQL + runbook), resultados de carga
.claude/commands/                    # Custom commands do livro (/po-*, /scaffold-*, /dev-*, /review-module, /test-*, /pre-release, /ops-*)
tests/
├── load/                            # k6: smoke, load, stress
├── TaskManager.Tasks.UnitTests/
├── TaskManager.Auth.UnitTests/
├── TaskManager.Notifications.UnitTests/
├── TaskManager.Api.IntegrationTests/
└── TaskManager.ArchitectureTests/
```

Build e testes saem em `./artifacts` (`UseArtifactsOutput`), não em `bin/obj` de cada projeto:
mantém os caminhos curtos o bastante para o limite de 260 caracteres do Windows.

## Licença

MIT — ver [LICENSE](LICENSE).

## Licenças de dependências

MediatR está fixado em 12.5.0 e FluentAssertions em 7.2.2, as últimas versões Apache 2.0.
As versões seguintes (MediatR 13+, FluentAssertions 8+) exigem licença comercial.

No frontend, todas as dependências são MIT/ISC/Apache 2.0. O `.npmrc` fixa versões exatas
(`save-exact`) e recusa pacotes publicados há menos de 7 dias (`min-release-age=7`) — a
janela em que um pacote sequestrado costuma ser detectado e removido.
