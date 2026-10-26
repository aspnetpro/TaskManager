Execute o checklist pré-release do TaskManager:

1. dotnet build (zero errors, zero warnings)
2. dotnet test (100% passando)
3. dotnet list package --vulnerable (zero vulns)
4. Verificar cobertura > 75%
5. Verificar OWASP gaps corrigidos
6. Verificar health checks respondendo
7. Verificar que CLAUDE.md está atualizado
8. Verificar zero TODOs críticos

Para cada item: PASS ou FAIL com detalhes.
Se algum item FAIL, liste as ações necessárias.
Salve resultado em docs/release-checklist-$ARGUMENTS.md
