#!/usr/bin/env bash
# Deploy de uma versão (tag = commit SHA) num ambiente. Usado pelos dois pipelines
# (Azure DevOps e GitHub Actions): a lógica mora aqui, o YAML só chama.
#
#   scripts/ci/deploy.sh <staging|production> <resource-group> <acr-login-server> <image-tag>
#
# Ordem: migrations (job) → Api → web → smoke test. Smoke falhou = volta Api e web
# para as imagens anteriores. Migrations NÃO voltam: por isso elas são sempre
# backward-compatible (expand-contract, docs/deploy.md).
set -euo pipefail

ENVIRONMENT="$1"
RESOURCE_GROUP="$2"
REGISTRY="$3"
TAG="$4"

NAME="taskmanager-${ENVIRONMENT}"
API_APP="ca-${NAME}-api"
WEB_APP="ca-${NAME}-web"
MIGRATIONS_JOB="caj-${NAME}-migrate"
# Sufixo único por execução: re-rodar o deploy do mesmo commit não colide com a revision anterior.
REVISION_SUFFIX="r${TAG:0:7}-$(date +%H%M%S)"

current_image() {
  az containerapp show -g "$RESOURCE_GROUP" -n "$1" --query "properties.template.containers[0].image" -o tsv
}

wait_for_revision() {
  local app="$1" revision="$1--${REVISION_SUFFIX}"
  for _ in $(seq 1 60); do
    local state
    state=$(az containerapp revision show -g "$RESOURCE_GROUP" -n "$app" --revision "$revision" \
      --query "properties.runningState" -o tsv 2>/dev/null || echo "Pending")
    case "$state" in
      Running|RunningAtMaxScale|ScaledToZero) echo "    $revision: $state"; return 0 ;;
      Failed|Degraded) echo "##[error]$revision: $state" >&2; return 1 ;;
    esac
    sleep 10
  done
  echo "##[error]$revision não ficou pronta em 10 minutos." >&2
  return 1
}

rollback() {
  echo "##[warning]Rollback: $API_APP → $PREVIOUS_API, $WEB_APP → $PREVIOUS_WEB"
  az containerapp update -g "$RESOURCE_GROUP" -n "$API_APP" --image "$PREVIOUS_API" --revision-suffix "rb-${REVISION_SUFFIX}" -o none
  az containerapp update -g "$RESOURCE_GROUP" -n "$WEB_APP" --image "$PREVIOUS_WEB" --revision-suffix "rb-${REVISION_SUFFIX}" -o none
}

PREVIOUS_API=$(current_image "$API_APP")
PREVIOUS_WEB=$(current_image "$WEB_APP")
echo "==> $ENVIRONMENT: $TAG (atual: ${PREVIOUS_API##*:})"

echo "==> migrations"
az containerapp job update -g "$RESOURCE_GROUP" -n "$MIGRATIONS_JOB" \
  --image "$REGISTRY/taskmanager-migrations:$TAG" -o none
EXECUTION=$(az containerapp job start -g "$RESOURCE_GROUP" -n "$MIGRATIONS_JOB" --query name -o tsv)
for _ in $(seq 1 90); do
  STATUS=$(az containerapp job execution show -g "$RESOURCE_GROUP" -n "$MIGRATIONS_JOB" \
    --job-execution-name "$EXECUTION" --query "properties.status" -o tsv)
  case "$STATUS" in
    Succeeded) echo "    $EXECUTION: ok"; break ;;
    Failed|Stopped|Degraded)
      echo "##[error]Migrations falharam ($EXECUTION: $STATUS). Nada foi trocado: Api e web seguem na versão anterior." >&2
      echo "    Logs: az containerapp job logs show -g $RESOURCE_GROUP -n $MIGRATIONS_JOB --execution $EXECUTION --container migrate" >&2
      exit 1 ;;
  esac
  sleep 10
done
[ "$STATUS" = "Succeeded" ] || { echo "##[error]Migrations sem resposta em 15 minutos." >&2; exit 1; }

# Daqui em diante, qualquer falha volta as imagens.
trap 'rollback' ERR

echo "==> api"
az containerapp update -g "$RESOURCE_GROUP" -n "$API_APP" \
  --image "$REGISTRY/taskmanager-api:$TAG" --revision-suffix "$REVISION_SUFFIX" -o none
wait_for_revision "$API_APP"

echo "==> web"
az containerapp update -g "$RESOURCE_GROUP" -n "$WEB_APP" \
  --image "$REGISTRY/taskmanager-web:$TAG" --revision-suffix "$REVISION_SUFFIX" -o none
wait_for_revision "$WEB_APP"

echo "==> smoke test"
WEB_URL="https://$(az containerapp show -g "$RESOURCE_GROUP" -n "$WEB_APP" --query properties.configuration.ingress.fqdn -o tsv)"
bash scripts/ci/smoke-test.sh "$WEB_URL"

trap - ERR
echo "==> $ENVIRONMENT em $TAG"
