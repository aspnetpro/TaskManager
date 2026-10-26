Crie um novo módulo chamado $ARGUMENTS no TaskManager
seguindo a estrutura Modular Monolith:

1. Crie os projetos:
   - TaskManager.$ARGUMENTS.Domain
   - TaskManager.$ARGUMENTS.Application
   - TaskManager.$ARGUMENTS.Infrastructure
   - TaskManager.$ARGUMENTS.Contracts

2. Configure referências entre projetos conforme regras
   do CLAUDE.md (Domain → Shared.Domain, Application → Domain
   + Contracts + Shared.*, Infrastructure → tudo do módulo)

3. Adicione à Solution e configure global usings

4. Crie DbContext do módulo em Infrastructure/Persistence/

5. Registre o módulo no Program.cs do Api

6. Rode dotnet build para verificar
