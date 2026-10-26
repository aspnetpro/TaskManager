using Microsoft.EntityFrameworkCore.Design;
using TaskManager.Shared.Domain;

namespace TaskManager.Tasks.Infrastructure.Persistence;

/// <summary>
/// Usado só por `dotnet ef migrations`. A connection string é placeholder:
/// gerar migration não conecta no banco.
/// </summary>
internal sealed class TasksDbContextFactory : IDesignTimeDbContextFactory<TasksDbContext>
{
    public TasksDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TasksDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=TaskManager;Integrated Security=true;TrustServerCertificate=true",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", TasksDbContext.Schema))
            .Options;

        return new TasksDbContext(options, new DesignTimeUser());
    }

    private sealed class DesignTimeUser : ICurrentUser
    {
        public Guid UserId => Guid.Empty;
        public Guid WorkspaceId => Guid.Empty;
        public WorkspaceRole Role => WorkspaceRole.Viewer;
    }
}
