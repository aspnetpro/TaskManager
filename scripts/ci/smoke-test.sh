#!/usr/bin/env bash
# Smoke test pós-deploy, sem credencial nenhuma: prova o caminho inteiro
# navegador → nginx → Api → banco, sem criar dado em produção.
#   scripts/ci/smoke-test.sh https://<fqdn-do-web>
set -euo pipefail
URL="$1"

expect() {
  local description="$1" expected="$2" actual
  shift 2
  for attempt in 1 2 3 4 5; do
    actual=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$@" || echo "000")
    [ "$actual" = "$expected" ] && { echo "    ok   $description ($actual)"; return 0; }
    sleep $((attempt * 3))
  done
  echo "##[error]falhou $description: esperado $expected, veio $actual" >&2
  return 1
}

expect "SPA (nginx)"               200 "$URL/"
expect "Api viva (via nginx)"      200 "$URL/healthz"
# Login inválido = 401: a Api respondeu, consultou o banco e mapeou o erro. 500 aqui = banco fora.
expect "Api + banco (login 401)"   401 -X POST "$URL/api/v1/auth/login" \
  -H "Content-Type: application/json" -d '{"email":"smoke-test@invalid.local","password":"not-a-real-password"}'
expect "Rota protegida exige token" 401 "$URL/api/v1/tasks"
