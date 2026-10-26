using System.Security.Claims;
using TaskManager.Auth.Contracts;
using TaskManager.Shared.Domain;

namespace TaskManager.Api.Security;

/// <summary>Lê as claims de workspace emitidas pelo módulo Auth (nomes em Auth.Contracts).</summary>
public static class WorkspaceClaims
{
    public const string UserId = WorkspaceClaimTypes.UserId;
    public const string WorkspaceId = WorkspaceClaimTypes.WorkspaceId;
    public const string WorkspaceRole = WorkspaceClaimTypes.WorkspaceRole;

    public static bool TryGetWorkspace(ClaimsPrincipal user, out Guid userId, out Guid workspaceId, out WorkspaceRole role)
    {
        userId = default;
        workspaceId = default;
        role = default;

        return Guid.TryParse(user.FindFirstValue(UserId), out userId)
            && Guid.TryParse(user.FindFirstValue(WorkspaceId), out workspaceId)
            && Enum.TryParse(user.FindFirstValue(WorkspaceRole), ignoreCase: true, out role)
            && Enum.IsDefined(role);
    }
}
