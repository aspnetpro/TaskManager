using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace TaskManager.Shared.Infrastructure.Persistence;

public static class DatabaseConfiguration
{
    public const string ConnectionStringName = "Database";

    /// <summary>
    /// SQL Server em todos os ambientes — dev via docker-compose, testes via
    /// Testcontainers. Sem provider InMemory: ele não traduz as mesmas queries
    /// que o SQL Server (ex: busca em complex types) e esconde bugs reais.
    /// Cada módulo usa seu próprio schema e sua própria tabela de migrations.
    /// </summary>
    public static DbContextOptionsBuilder UseModuleDatabase(
        this DbContextOptionsBuilder options,
        IConfiguration configuration,
        string schema)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{ConnectionStringName} is not configured. " +
                "In development: docker compose up -d && dotnet user-secrets set " +
                $"\"ConnectionStrings:{ConnectionStringName}\" \"<connection string>\" (see README).");
        }

        return options.UseSqlServer(connectionString, sql => sql
            .MigrationsHistoryTable("__EFMigrationsHistory", schema)
            .EnableRetryOnFailure());
    }
}
