Crie uma nova feature no TaskManager.
Argumento: {Modulo}/{Acao} (ex: Tasks/Complete)

1. Na camada Application, crie Features/{Modulo}/{Acao}/:
   - {Acao}{Modulo}Command.cs (ou Query.cs) como record
   - {Acao}{Modulo}CommandValidator.cs com FluentValidation
   - {Acao}{Modulo}Handler.cs retornando Result<T>
   - {Modulo}Response.cs (se não existir)

2. Na camada Api, adicione o endpoint ao MapGroup
   existente do módulo

3. Crie teste unitário para o Handler e Validator
   em UnitTests/Features/{Modulo}/{Acao}/

4. Siga TODOS os padrões do CLAUDE.md
5. Rode dotnet build && dotnet test
