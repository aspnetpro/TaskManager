using Microsoft.EntityFrameworkCore;
using TaskManager.Notifications.Domain;
using TaskManager.Shared.Application;

namespace TaskManager.Notifications.Infrastructure.Persistence;

/// <summary>
/// Dois filtros: workspace (tenant) e destinatário. Notificação é do usuário,
/// não do workspace — um Admin não lê as notificações dos outros.
/// Os consumidores do Outbox só gravam, então nunca avaliam os filtros sem usuário.
/// </summary>
public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options, ICurrentUser currentUser)
    : DbContext(options)
{
    public const string Schema = "notifications";

    public DbSet<Notification> Notifications => Set<Notification>();

    private Guid CurrentWorkspaceId => currentUser.WorkspaceId;
    private Guid CurrentUserId => currentUser.UserId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);

        modelBuilder.Entity<Notification>()
            .HasQueryFilter(NotificationQueryFilters.Tenant, n => n.WorkspaceId == CurrentWorkspaceId)
            .HasQueryFilter(NotificationQueryFilters.Recipient, n => n.RecipientId == CurrentUserId);
    }
}
