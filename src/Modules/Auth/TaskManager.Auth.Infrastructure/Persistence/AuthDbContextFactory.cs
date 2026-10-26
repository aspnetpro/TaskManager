using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManager.Auth.Infrastructure.Persistence;

/// <summary>Usado só por `dotnet ef migrations`. Connection string placeholder.</summary>
internal sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=TaskManager;Integrated Security=true;TrustServerCertificate=true",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", AuthDbContext.Schema))
            .Options;

        return new AuthDbContext(options);
    }
}
