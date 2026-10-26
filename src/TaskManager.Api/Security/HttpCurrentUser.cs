using TaskManager.Shared.Application;
using TaskManager.Shared.Domain;

namespace TaskManager.Api.Security;

internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private readonly Lazy<(Guid UserId, Guid WorkspaceId, WorkspaceRole Role)> _identity = new(() =>
    {
        var user = httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("No HTTP context: ICurrentUser is only available during a request.");

        // A fallback policy (WorkspaceMember) garante essas claims em todo endpoint autenticado.
        return WorkspaceClaims.TryGetWorkspace(user, out var userId, out var workspaceId, out var role)
            ? (userId, workspaceId, role)
            : throw new InvalidOperationException("The authenticated user has no valid workspace claims.");
    });

    public Guid UserId => _identity.Value.UserId;
    public Guid WorkspaceId => _identity.Value.WorkspaceId;
    public WorkspaceRole Role => _identity.Value.Role;
}
