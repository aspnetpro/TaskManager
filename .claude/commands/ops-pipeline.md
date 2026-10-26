Gere ou atualize a pipeline CI/CD para $ARGUMENTS.
Inclua: build, test com cobertura, security scan (NuGet, npm,
gitleaks), docker build, Trivy, push para ACR com tag = SHA,
deploy em staging (auto) e production (com approval).
A lógica de deploy fica em scripts/ci/; o YAML só chama.
Nenhum secret no pipeline: OIDC + identidade gerenciada.
