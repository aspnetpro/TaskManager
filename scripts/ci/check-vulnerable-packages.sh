#!/usr/bin/env bash
# Falha se algum pacote NuGet (inclusive transitivo) tem vulnerabilidade conhecida.
# O NuGet Audit já quebra o restore (TreatWarningsAsErrors); isto é a segunda rede,
# rodando mesmo quando o restore veio do cache.
set -euo pipefail

output=$(dotnet list TaskManager.slnx package --vulnerable --include-transitive 2>&1)
echo "$output"

if grep -q "has the following vulnerable packages" <<< "$output"; then
  echo "##[error]Pacotes NuGet vulneráveis encontrados." >&2
  exit 1
fi
