#!/usr/bin/env bash
# Testes com cobertura (Coverlet) + relatório (ReportGenerator) com thresholds.
# Uso: scripts/ci/coverage.sh [filtro-do-dotnet-test]
#   scripts/ci/coverage.sh                          # tudo (integração exige Docker)
#   scripts/ci/coverage.sh "Category!=Integration"  # só unitários
set -euo pipefail

FILTER="${1:-}"
RESULTS=./coverage
rm -rf "$RESULTS"

dotnet tool restore
dotnet test TaskManager.slnx --configuration Release \
  ${FILTER:+--filter "$FILTER"} \
  --settings coverage.runsettings \
  --collect "XPlat Code Coverage" \
  --results-directory "$RESULTS" \
  --logger trx

# Global: 75% de linhas. Abaixo disso o comando sai com erro e quebra o pipeline.
# Atenção ao "--": sem ele o ReportGenerator ignora o threshold e sai com 0 (gate falso).
dotnet reportgenerator \
  -reports:"$RESULTS/**/coverage.cobertura.xml" \
  -targetdir:"$RESULTS/report" \
  -reporttypes:"Html;Cobertura;MarkdownSummaryGithub" \
  --minimumCoverageThresholds:lineCoverage=75

# Domínio do Tasks: é onde as regras de negócio moram. 90%.
dotnet reportgenerator \
  -reports:"$RESULTS/**/coverage.cobertura.xml" \
  -targetdir:"$RESULTS/report-tasks-domain" \
  -reporttypes:"TextSummary" \
  -assemblyfilters:"+TaskManager.Tasks.Domain" \
  --minimumCoverageThresholds:lineCoverage=90

cat "$RESULTS/report-tasks-domain/Summary.txt"
