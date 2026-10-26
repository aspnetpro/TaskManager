Crie ou revise o Dockerfile de $ARGUMENTS.
Multi-stage com cache mount; runtime chiseled (ou unprivileged),
não-root, sem ferramentas que o runtime não usa.
Teste: docker build, docker run --read-only, e Trivy
(CRITICAL/HIGH com correção = falha).
