#!/bin/sh
# Entrypoint do job de migrations (Container Apps Job, Cap. 13).
# 1. Aplica as migrations dos três módulos (EF Core bundles, idempotentes e com lock).
# 2. Garante o usuário da Api no banco com o mínimo de privilégio (tools/TaskManager.DbAccess).
# Roda com a identidade do migrator, que é o admin Entra do SQL Server — a Api não é.
set -eu

for module in auth tasks notifications; do
  echo "==> migrations: $module"
  "/migrations/efbundle-$module" --connection "$ConnectionStrings__Database"
done

if [ -n "${API_IDENTITY_NAME:-}" ]; then
  echo "==> acesso da Api: $API_IDENTITY_NAME"
  dotnet /migrations/dbaccess/TaskManager.DbAccess.dll
fi

echo "==> ok"
