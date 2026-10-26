using Microsoft.EntityFrameworkCore;
using TaskManager.Auth.Application.Abstractions;
using TaskManager.Auth.Domain.Entities;
using TaskManager.Auth.Infrastructure.Persistence;
using TaskManager.Shared.Domain;

namespace TaskManager.Auth.Infrastructure;

/// <summary>
/// Workspace de demonstração para desenvolvimento local, com um usuário por
/// papel. A senha vem de configuração (DemoWorkspace:Password). Nunca roda fora de Development.
/// </summary>
public static class DemoWorkspace
{
    public const string PasswordConfigKey = "DemoWorkspace:Password";

    public static readonly Guid WorkspaceId = Guid.Parse("01970000-0000-7000-8000-000000000001");
    public static readonly Guid AdminId = Guid.Parse("01970000-0000-7000-8000-0000000000a1");
    public static readonly Guid TechLeadId = Guid.Parse("01970000-0000-7000-8000-0000000000b2");
    public static readonly Guid DeveloperId = Guid.Parse("01970000-0000-7000-8000-0000000000c3");
    public static readonly Guid ViewerId = Guid.Parse("01970000-0000-7000-8000-0000000000d4");

    private static readonly (Guid Id, string Email, string Name, WorkspaceRole Role)[] Members =
    [
        (AdminId, "admin@demo.local", "Demo Admin", WorkspaceRole.Admin),
        (TechLeadId, "techlead@demo.local", "Demo Tech Lead", WorkspaceRole.TechLead),
        (DeveloperId, "developer@demo.local", "Demo Developer", WorkspaceRole.Developer),
        (ViewerId, "viewer@demo.local", "Demo Viewer", WorkspaceRole.Viewer)
    ];

    /// <summary>Idempotente: cria o que falta (bancos da Fase 1 já têm o workspace, mas não os usuários).</summary>
    public static async Task SeedAsync(
        AuthDbContext context,
        IPasswordHasher passwordHasher,
        string password,
        CancellationToken cancellationToken = default)
    {
        var workspace = await context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == WorkspaceId, cancellationToken);

        if (workspace is null)
        {
            workspace = Workspace.Create("Demo Workspace", WorkspaceId);
            context.Workspaces.Add(workspace);
        }

        var memberIds = Members.Select(m => m.Id).ToArray();
        var existingUsers = await context.Users
            .Where(u => memberIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var hash = passwordHasher.Hash(password);
        foreach (var (id, email, name, role) in Members)
        {
            if (!existingUsers.Contains(id))
                context.Users.Add(User.Create(email, name, hash, id));

            // AlreadyMember em banco existente é esperado: ignora.
            workspace.AddMember(id, role);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
