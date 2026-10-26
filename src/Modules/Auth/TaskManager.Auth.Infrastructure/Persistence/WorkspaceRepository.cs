using Microsoft.EntityFrameworkCore;
using TaskManager.Auth.Application.Abstractions;
using TaskManager.Auth.Application.Features.Members;
using TaskManager.Auth.Domain.Entities;

namespace TaskManager.Auth.Infrastructure.Persistence;

internal sealed class WorkspaceRepository(AuthDbContext context) : IWorkspaceRepository
{
    public void Add(Workspace workspace) => context.Workspaces.Add(workspace);

    public Task<Workspace?> GetWithMembersAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

    public async Task<IReadOnlyList<MembershipSummary>> GetActiveMembershipsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        // Guid v7: ordenar pelo Id do workspace é ordenar pela data de criação.
        var rows = await (
                from member in context.WorkspaceMembers.AsNoTracking()
                join workspace in context.Workspaces on member.WorkspaceId equals workspace.Id
                where member.UserId == userId && member.IsActive
                orderby workspace.Id
                select new { workspace.Id, workspace.Name, member.Role })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(r => new MembershipSummary(r.Id, r.Name, r.Role));
    }

    public async Task<IReadOnlyList<MemberResponse>> ListMembersAsync(
        Guid workspaceId, CancellationToken cancellationToken = default)
    {
        // Left join: membro sem conta (dados antigos ou de teste) aparece com email vazio.
        var rows = await (
                from member in context.WorkspaceMembers.AsNoTracking()
                join user in context.Users on member.UserId equals user.Id into users
                from user in users.DefaultIfEmpty()
                where member.WorkspaceId == workspaceId
                orderby user.FullName
                select new
                {
                    member.UserId,
                    Email = user == null ? string.Empty : user.Email,
                    FullName = user == null ? string.Empty : user.FullName,
                    member.Role,
                    member.IsActive
                })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(r => new MemberResponse(r.UserId, r.Email, r.FullName, r.Role, r.IsActive));
    }
}
