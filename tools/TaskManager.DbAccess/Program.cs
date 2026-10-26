using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

// Garante o usuário da Api no banco com o mínimo de privilégio (read/write, sem DDL).
// Roda no job de migrations, depois das bundles, com a identidade do migrator (admin
// Entra do SQL). Idempotente: roda a cada deploy.
//
// CREATE USER ... WITH SID + TYPE = E cria o usuário Entra sem consultar o Microsoft
// Graph: o SQL Server não precisa da role Directory Readers. Para identidade gerenciada,
// o SID é o client id. Guid.ToByteArray() tem o mesmo layout do uniqueidentifier do SQL.

var connectionString = Required("ConnectionStrings__Database");
var identityName = Required("API_IDENTITY_NAME");
var clientId = Guid.Parse(Required("API_IDENTITY_CLIENT_ID"));

// O nome vai concatenado no DDL (CREATE USER não aceita parâmetro): só caracteres de nome de recurso do Azure.
if (!Regex.IsMatch(identityName, "^[A-Za-z0-9_-]{3,128}$"))
    throw new InvalidOperationException($"API_IDENTITY_NAME inválido: '{identityName}'.");

var sid = "0x" + Convert.ToHexString(clientId.ToByteArray());
var sql = $"""
    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @name)
        CREATE USER [{identityName}] WITH SID = {sid}, TYPE = E;
    ALTER ROLE db_datareader ADD MEMBER [{identityName}];
    ALTER ROLE db_datawriter ADD MEMBER [{identityName}];
    """;

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
await using var command = new SqlCommand(sql, connection);
command.Parameters.AddWithValue("@name", identityName);
await command.ExecuteNonQueryAsync();

Console.WriteLine($"Acesso da Api garantido: {identityName} (db_datareader, db_datawriter).");

static string Required(string name) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Variável de ambiente {name} não definida.");
