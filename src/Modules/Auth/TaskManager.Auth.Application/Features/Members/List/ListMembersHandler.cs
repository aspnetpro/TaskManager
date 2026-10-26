namespace TaskManager.Auth.Application.Features.Members.List;

public sealed class ListMembersHandler(IWorkspaceRepository workspaces, ICurrentUser currentUser)
    : IRequestHandler<ListMembersQuery, Result<IReadOnlyList<MemberResponse>>>
{
    public async Task<Result<IReadOnlyList<MemberResponse>>> Handle(ListMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await workspaces.ListMembersAsync(currentUser.WorkspaceId, cancellationToken);
        return Result<IReadOnlyList<MemberResponse>>.Success(members);
    }
}
