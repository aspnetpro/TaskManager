# syntax=docker/dockerfile:1
# Imagem da Api. A mesma imagem roda no compose local e no Azure Container Apps (Cap. 13).
# Testes NÃO rodam aqui: o pipeline roda antes (os de integração precisam de Docker,
# e Docker dentro do build da imagem é gambiarra). Imagem só nasce de commit testado.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Commit que gerou a imagem: vira InformationalVersion (1.0.0+sha) e service.version na telemetria.
ARG SOURCE_REVISION=local

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/ src/

# Cache mount: os pacotes NuGet sobrevivem entre builds mesmo quando o código muda.
# Faz o papel do "copie só os .csproj primeiro", sem listar os 16 projetos à mão.
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/TaskManager.Api/TaskManager.Api.csproj \
      -c Release -o /app \
      -p:SourceRevisionId=${SOURCE_REVISION} \
      -p:UseAppHost=false

# Migrations (docker build --target migrations): EF Core bundles dos três módulos +
# tools/TaskManager.DbAccess (grant da Api no banco). Roda como Container Apps Job
# dentro da VNet: o pipeline nunca precisa enxergar o banco.
FROM build AS bundles
COPY dotnet-tools.json ./
COPY tools/ tools/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet tool restore && \
    for module in Auth Tasks Notifications; do \
      dotnet ef migrations bundle --configuration Release \
        --project src/Modules/$module/TaskManager.$module.Infrastructure \
        --startup-project src/TaskManager.Api \
        --context ${module}DbContext \
        -o /bundles/efbundle-$(echo $module | tr '[:upper:]' '[:lower:]') || exit 1; \
    done && \
    dotnet publish tools/TaskManager.DbAccess -c Release -o /bundles/dbaccess -p:UseAppHost=false

# Bundles framework-dependent: o runtime vem da imagem (aspnet, porque o startup project
# é a Api). Self-contained eram 3 cópias do runtime: imagem de 1 GB. Não chiseled: o
# script precisa de /bin/sh. A bundle é single-file e extrai arquivos no startup: em /tmp,
# que é gravável mesmo com o filesystem do container somente leitura.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS migrations
ENV DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp
COPY --from=bundles /bundles /migrations
COPY deploy/migrations/migrate.sh /migrations/
USER $APP_UID
ENTRYPOINT ["/bin/sh", "/migrations/migrate.sh"]

# Chiseled: sem shell, sem gerenciador de pacotes, usuário não-root (app, UID 1654) por padrão.
# "-extra" traz ICU e tzdata: a chiseled pura roda em globalization invariant mode
# e o SqlClient se recusa a abrir conexão ("Globalization Invariant Mode is not supported").
# Sem shell não há curl: HEALTHCHECK de Docker não existe aqui. Quem checa saúde é o
# orquestrador (probes do Container Apps em /health/live e /health/ready).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS runtime
ARG SOURCE_REVISION=local
LABEL org.opencontainers.image.revision="${SOURCE_REVISION}" \
      org.opencontainers.image.title="taskmanager-api"
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]
