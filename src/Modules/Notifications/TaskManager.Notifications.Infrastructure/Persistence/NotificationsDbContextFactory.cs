using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TaskManager.Shared.Application;
using TaskManager.Shared.Domain;

namespace TaskManager.Notifications.Infrastructure.Persistence;

/// <summary>Usado só por `dotnet ef migrations`. Connection string placeholder.</summary>
internal sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=TaskManager;Integrated Security=true;TrustServerCertificate=true",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", NotificationsDbContext.Schema))
            .Options;

        return new NotificationsDbContext(options, new DesignTimeUser());
    }

    private sealed class DesignTimeUser : ICurrentUser
    {
        public Guid UserId => Guid.Empty;
        public Guid WorkspaceId => Guid.Empty;
        public WorkspaceRole Role => WorkspaceRole.Viewer;
    }
}
