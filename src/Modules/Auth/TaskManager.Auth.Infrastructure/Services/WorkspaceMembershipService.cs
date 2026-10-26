using Microsoft.EntityFrameworkCore;
using TaskManager.Auth.Contracts;
using TaskManager.Auth.Infrastructure.Persistence;

namespace TaskManager.Auth.Infrastructure.Services;

internal sealed class WorkspaceMembershipService(AuthDbContext context) : IWorkspaceMembershipService
{
    public Task<bool> IsActiveMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default) =>
        context.WorkspaceMembers
            .AsNoTracking()
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.IsActive, cancellationToken);
}
