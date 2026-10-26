using Microsoft.AspNetCore.Authorization;
using TaskManager.Shared.Domain;

namespace TaskManager.Api.Security;

/// <summary>
/// Lê o role direto do ClaimsPrincipal do contexto de autorização — não de um
/// serviço scoped, que pode nem ter HttpContext quando a policy é avaliada.
/// </summary>
public sealed class WorkspaceRoleHandler : AuthorizationHandler<WorkspaceRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkspaceRoleRequirement requirement)
    {
        if (WorkspaceClaims.TryGetWorkspace(context.User, out _, out _, out var role) &&
            (requirement.AllowedRoles.Count == 0 || requirement.AllowedRoles.Contains(role)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
