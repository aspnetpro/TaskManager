using Microsoft.AspNetCore.Authorization;
using TaskManager.Shared.Domain;

namespace TaskManager.Api.Security;

public sealed class WorkspaceRoleRequirement(params WorkspaceRole[] allowedRoles) : IAuthorizationRequirement
{
    public IReadOnlyCollection<WorkspaceRole> AllowedRoles { get; } = allowedRoles;
}
