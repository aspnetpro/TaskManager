using TaskManager.Shared.Infrastructure.Outbox;

namespace TaskManager.Tasks.Infrastructure.Persistence;

public sealed class TasksDbContext(DbContextOptions<TasksDbContext> options, ICurrentUser currentUser)
    : DbContext(options)
{
    public const string Schema = "tasks";

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    // Lido a cada execução de query (o EF parametriza membros do DbContext).
    private Guid CurrentWorkspaceId => currentUser.WorkspaceId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TasksDbContext).Assembly);
        modelBuilder.AddOutbox();

        // Named query filters (EF Core 10): podem ser desligados individualmente.
        // IgnoreQueryFilters() sem argumentos desligaria o tenant também — IDOR.
        modelBuilder.Entity<TaskItem>()
            .HasQueryFilter(TaskQueryFilters.Tenant, t => t.WorkspaceId == CurrentWorkspaceId)
            .HasQueryFilter(TaskQueryFilters.SoftDelete, t => !t.IsDeleted);
    }
}
