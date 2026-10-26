using Microsoft.EntityFrameworkCore;
using TaskManager.Auth.Domain.Entities;

namespace TaskManager.Auth.Infrastructure.Persistence;

/// <summary>
/// Sem filtro de tenant: o Auth é o diretório de usuários e workspaces, e
/// login/refresh rodam antes de existir um workspace na requisição. Cada
/// query do módulo filtra por workspace/usuário explicitamente.
/// </summary>
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public const string Schema = "auth";

    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
