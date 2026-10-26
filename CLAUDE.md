# CLAUDE.md - TaskManager

## Visão Geral
SaaS B2B de gerenciamento de tarefas para times de dev. Multi-tenant por workspace.
Modular Monolith com .NET 10, Clean Architecture, Vertical Slices.
Deploy: Azure Container Apps. DB: SQL Server / Azure SQL via EF Core 10.

## Comandos
- Build: `dotnet build` (TreatWarningsAsErrors: warning quebra o build)
- Testes rápidos: `dotnet test --filter Category!=Integration`
- Todos os testes: `dotnet test` (integração exige Docker rodando — Testcontainers)
- Dependências locais (SQL Server + Redis): `docker compose up -d`; Api + frontend em container também: `docker compose --profile app up -d --build` (SPA em :3000)
- Frontend (em `frontend/`): `npm run dev` (proxy /api → :5000), `npm test`, `npm run typecheck`, `npm run lint` (oxlint), `npm run build`
- Secrets de dev: `dotnet user-secrets` em src/TaskManager.Api (`ConnectionStrings:Database`, `Jwt:SigningKey`)
- Saída de build em `./artifacts` (UseArtifactsOutput) — não procure bin/obj nos projetos
- Migration: `dotnet ef migrations add <Nome> --project src/Modules/<Modulo>/TaskManager.<Modulo>.Infrastructure --startup-project src/TaskManager.Api --context <Modulo>DbContext --output-dir Persistence/Migrations`
- Cobertura: `bash scripts/ci/coverage.sh` (gate: 75% global, 90% Tasks.Domain; `--minimumCoverageThresholds` com DOIS hífens, senão é ignorado)
- Telemetria local: Aspire Dashboard em http://localhost:18888 (sobe com `docker compose up -d`)
- Carga: `tests/load/` (k6 via Docker; suba a stack com `AUTH_PERMIT_LIMIT`/`GLOBAL_PERMIT_LIMIT` altos)
- Imagens: `docker build --target runtime|migrations .` e `docker build frontend`; rode com `--read-only` e passe no Trivy antes de commitar
- Infra: `az bicep build --file .azure/infra/main.bicep` e `az bicep lint` sem warnings

## Arquitetura
- Módulos: Tasks, Auth, Notifications (Reporting só quando houver requisito)
- Cada módulo: Domain → Application → Infrastructure, + Contracts (API pública do módulo: interfaces e integration events)
- Shared Kernel: Shared.Domain (Entity, Result, Error, WorkspaceRole), Shared.Application (behaviors, ICurrentUser, IUnitOfWork, IWorkspaceCache, IIntegrationEventHandler), Shared.Infrastructure (interceptors, EfUnitOfWork, Outbox, HybridCache)
- Regras de dependência são verificadas por `tests/TaskManager.ArchitectureTests` — se um teste de arquitetura falhar, corrija o código, nunca o teste

| Projeto | Pode referenciar | NÃO pode referenciar |
| --- | --- | --- |
| X.Domain | Shared.Domain | Application, Infrastructure, EF Core, MediatR, outros módulos |
| X.Application | X.Domain, Shared.Application, **Contracts de outros módulos** | Infrastructure, EF Core, ASP.NET, Domain de outros módulos |
| X.Infrastructure | X.Application, Shared.Infrastructure | Api, Infrastructure de outros módulos |
| X.Contracts | nada (só BCL) | qualquer projeto do solution |
| Api (composition root) | Application e Infrastructure dos módulos | — endpoints não tocam EF Core nem entidades |

## Stack
- .NET 10 LTS, C# 14, ASP.NET Minimal APIs (sem controllers)
- EF Core 10 + SQL Server em todos os ambientes (sem provider InMemory)
- MediatR 12.5 (Apache 2.0) — NÃO atualizar para 13+ (licença comercial)
- FluentValidation 12
- Testes: xUnit + FluentAssertions 7.2 (Apache 2.0 — NÃO atualizar para 8+) + NSubstitute + Testcontainers + NetArchTest
- Versões centralizadas em Directory.Packages.props

## Convenções de Código
- Código em inglês; docs, comentários e commits em português
- File-scoped namespaces; primary constructors para DI
- Records para DTOs, Commands, Queries e Value Objects
- Um tipo por arquivo, arquivo com o nome do tipo
- GlobalUsings.cs por projeto
- Guid v7 (`Guid.CreateVersion7()`) para Ids
- `TimeProvider` injetado para qualquer "agora" — nunca `DateTime.Now`/`UtcNow` em código de produção

## Result Pattern
- Handler SEMPRE retorna `Result` ou `Result<T>` (Shared.Domain.Results)
- Erros de negócio são `Error` com `ErrorType`; nunca exceção
- Mapeamento HTTP (Api/Http/ResultExtensions): Validation→400, Unauthorized→401, Forbidden→403, NotFound→404, Conflict→409, BusinessRule→422, Unexpected→500
- EfUnitOfWork converte concorrência e violação de índice único em Conflict (`CommonErrors`) — corrida previsível não é 500
- Retorne o erro direto: `return TaskErrors.NotFound(id);` (conversão implícita)

## Multi-tenancy (CRÍTICO)
- Toda entidade de negócio tem `WorkspaceId`
- O DbContext do módulo aplica named query filter "Tenant" com o workspace do `ICurrentUser`
- Para ver excluídos: `IgnoreQueryFilters(["SoftDelete"])` — NUNCA `IgnoreQueryFilters()` sem argumentos (desliga o tenant = IDOR)
- Recurso de outro workspace retorna 404, não 403
- Cache só via `IWorkspaceCache`: ele prefixa a chave com o workspace. Chave sem tenant = IDOR via cache
- Exceção consciente: AuthDbContext não tem filtro de tenant (é o diretório de usuários; login roda sem workspace). Toda query do Auth filtra explicitamente

## Auth
- JWT próprio (HS256) emitido pelo módulo Auth; a Api valida com as mesmas `JwtOptions` (seção `Jwt`, chave em user-secrets/Key Vault)
- Access token 15 min no body; refresh token 7 dias só no cookie `tm_refresh` (httpOnly, Secure, SameSite=Strict, Path=/api/v1/auth) — nunca no body
- Refresh rotaciona sempre; token já rotacionado reapresentado revoga a família inteira
- Senha: PasswordHasher do Identity (PBKDF2) sem as stores EF do Identity; `User` é entidade nossa
- Claims em `Auth.Contracts.WorkspaceClaimTypes` — não duplique os nomes

## Eventos entre módulos (Outbox)
- Domain event é interno; o que outro módulo consome vira integration event em `X.Contracts/IntegrationEvents` (record, só BCL, com `EventId`)
- O `XOutboxInterceptor` do módulo mapeia domain → integration event e grava no Outbox do schema do módulo, no mesmo SaveChanges
- Consumidor: `IIntegrationEventHandler<T>` registrado com `AddIntegrationEventHandler<TEvent, THandler>()`
- Entrega at-least-once: handler idempotente (índice único por `EventId`); falha = exceção, e o Outbox tenta de novo (até `Outbox:MaxAttempts`)

## Vertical Slices
- Features/{Entidade}/{Acao}/: Command|Query, Validator, Handler
- Response do slice fica em Application (Features/{Entidade}/), não em Contracts
- Validator com FluentValidation; o ValidationBehavior roda antes do handler
- Regras de negócio na entidade (ex: `TaskItem.TransitionTo`), não no handler
- Unit of Work por módulo (`ITasksUnitOfWork`) — nunca injetar `IUnitOfWork` genérico

## Endpoints
- Minimal APIs com MapGroup por entidade em Api/Endpoints
- `/api/v1/...`; erros como ProblemDetails (RFC 9457) com extensão `code`
- Enums como string no JSON (inteiro é rejeitado com 400)
- Concorrência otimista: GET devolve `ETag` com a `Version`; PATCH exige `If-Match` (428 sem, 409 se desatualizado)
- Autorização: fallback policy exige membro de workspace; policies `CanWriteTasks` e `AdminOnly`; endpoint anônimo precisa de `AllowAnonymous()` explícito
- Rate limiting: limite global por usuário/IP; endpoints de auth com `RequireRateLimiting(RateLimitingExtensions.AuthPolicy)`
- Commands que alteram algo cacheado implementam `IInvalidatesCache` (o CacheInvalidationBehavior remove as chaves)

## Frontend (frontend/)
- React 19 + TypeScript strict + Vite 8 + Tailwind 4 (tema em `src/index.css`, `@theme`), TanStack Query 5, Zustand, React Hook Form + Zod 4, React Router 8 (data mode, rotas lazy), @dnd-kit/core
- HTTP só via `src/api/http.ts` (`request<T>`): fetch nativo, Bearer do store, 401 → `refreshSession()` single-flight com Web Lock entre abas. NUNCA chame `/auth/refresh` por fora dele (refresh paralelo = reuso de token = família revogada)
- Access token só em memória (Zustand); NUNCA localStorage/sessionStorage. O refresh vive no cookie httpOnly
- `src/types/api.ts` espelha os records C#: mudou um record, muda aqui no mesmo commit
- Regras duplicadas do backend só para UX (`lib/transitions.ts`, `lib/permissions.ts`, schemas Zod) — a Api continua sendo a fonte de verdade
- Query keys só via `lib/queryKeys.ts`; o cache é limpo quando usuário/workspace muda (`clearCacheOnSessionChange`)
- Erro para o usuário via `describeError` (traduz o `code` do ProblemDetails); erro 400 por campo volta ao campo com `fieldErrorsOf`
- Filtros de tela moram na URL (`useUrlFilters`)
- UI em português; código em inglês. Componente < 200 linhas
- Testes: Vitest + Testing Library + MSW 3 (`src/test/server.ts` é a Api fake). Teste o comportamento visível, não implementação
- `npm install` respeita `.npmrc` (`save-exact`, `min-release-age=7`) — não contorne

## Observabilidade
- Logs: Serilog (`AddApiObservability`); scope do `ILogger` para dimensões de request (o LogContext do Serilog não chega ao OpenTelemetry)
- Correlation id é o TraceId (W3C). Não crie header próprio
- Métricas de negócio no Application do módulo, só BCL (`IMeterFactory`), nome `taskmanager.<area>.<metrica>`; registre o meter em `ObservabilityExtensions`. NUNCA workspace/usuário como tag (cardinalidade)
- Span por caso de uso vem do `TracingBehavior`; erro de negócio é tag, status Error só para Unexpected
- Destino da telemetria é configuração: `OTEL_EXPORTER_OTLP_ENDPOINT` (Aspire) e/ou `APPLICATIONINSIGHTS_CONNECTION_STRING`
- Nunca logar email, senha, token, connection string

## Deploy (docs/deploy.md)
- Tag de imagem = SHA do commit; nunca `latest`
- Lógica de pipeline em `scripts/ci/` — Azure DevOps e GitHub Actions só chamam. Scripts com LF (`.gitattributes`)
- Zero secret no pipeline: OIDC + identidade gerenciada; segredo da Api só no Key Vault
- Migrations rodam no Container Apps Job (`--target migrations`), nunca do agente do pipeline. Toda migration é backward-compatible (expand-contract): rollback de imagem não desfaz schema
- Configuração por ambiente: variável no Bicep ou Key Vault. Sem `appsettings.{Ambiente}.json`
- nginx: todos os `proxy_set_header` no nível do server (um no location descarta os herdados); upstream com `keepalive`

## Testes
- Naming: Metodo_Cenario_ResultadoEsperado
- Unitários: domínio e handlers com NSubstitute + FakeTimeProvider; TaskItemBuilder para dados
- Integração: `TaskManager.Api.IntegrationTests` — WebApplicationFactory + Testcontainers (SQL Server real), `[Trait("Category","Integration")]`
- Cada teste de integração cria o próprio workspace (`factory.CreateWorkspaceAsync()`) ou registra o próprio usuário (`AuthApi.RegisterAsync`)
- Headers X-Test-* = TestAuthHandler; sem eles, JWT real. Fluxos via Outbox são eventuais: espere o efeito, não durma tempo fixo
- Arrange / Act / Assert explícito; [Theory] para múltiplos cenários

## Git
- Conventional Commits: feat:, fix:, refactor:, test:, docs:, chore:
- Branch: feature/xxx, bugfix/xxx, refactor/xxx
- Rodar `dotnet test` antes de commitar

## O que NUNCA fazer
- NUNCA usar exceções para fluxo de negócio
- NUNCA criar navigation properties entre módulos (referência só por Id)
- NUNCA acessar DbContext de outro módulo — use a interface em Contracts
- NUNCA hardcodar connection strings ou secrets (dev: `dotnet user-secrets`; prod: Key Vault)
- NUNCA usar o provider EF InMemory (diverge do SQL Server e esconde bugs)
- NUNCA aplicar migrations no startup fora de Development
- NUNCA criar classes com mais de 200 linhas
- NUNCA usar `ASPNETCORE_FORWARDEDHEADERS_ENABLED` (confia em qualquer origem e lê só um salto): use `ForwardedHeaders:KnownNetworks`
