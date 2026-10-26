#!/usr/bin/env bash
# Provisiona/atualiza a infra de um ambiente (Bicep) sem derrubar a versão em execução:
# as imagens atuais são lidas dos Container Apps e repassadas ao template.
#
#   scripts/ops/deploy-infra.sh <staging|production> <resource-group> [--bootstrap]
#
# --bootstrap (só no primeiro deploy do ambiente): gera a chave JWT e grava no Key Vault.
# Rodar de novo com --bootstrap ROTACIONA a chave e desloga todo mundo.
# Variáveis: ACR_NAME, ACR_RESOURCE_GROUP, ALERT_EMAIL (lidas pelos .bicepparam).
set -euo pipefail

ENVIRONMENT="$1"
RESOURCE_GROUP="$2"
BOOTSTRAP="${3:-}"
NAME="taskmanager-${ENVIRONMENT}"
INFRA="$(dirname "$0")/../../.azure/infra"

# Imagem em execução (vazio se o recurso ainda não existe = primeiro deploy, usa o placeholder).
app_image() {
  az containerapp show -g "$RESOURCE_GROUP" -n "$1" --query "properties.template.containers[0].image" -o tsv 2>/dev/null || true
}
job_image() {
  az containerapp job show -g "$RESOURCE_GROUP" -n "$1" --query "properties.template.containers[0].image" -o tsv 2>/dev/null || true
}

args=()
api=$(app_image "ca-${NAME}-api");      [ -n "$api" ] && args+=("apiImage=$api")
web=$(app_image "ca-${NAME}-web");      [ -n "$web" ] && args+=("webImage=$web")
job=$(job_image "caj-${NAME}-migrate"); [ -n "$job" ] && args+=("migrationsImage=$job")

if [ "$BOOTSTRAP" = "--bootstrap" ]; then
  echo "==> bootstrap: gerando Jwt--SigningKey"
  args+=("jwtSigningKey=$(openssl rand -base64 48)")
fi

az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --name "taskmanager-${ENVIRONMENT}-$(date +%Y%m%d%H%M%S)" \
  --template-file "$INFRA/main.bicep" \
  --parameters "$INFRA/parameters/${ENVIRONMENT}.bicepparam" \
  ${args[@]+--parameters "${args[@]}"} \
  --query properties.outputs -o json
