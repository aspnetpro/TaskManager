namespace TaskManager.Auth.Application.Features.Members.Add;

public sealed class AddMemberHandler(
    IUserRepository users,
    IWorkspaceRepository workspaces,
    ICurrentUser currentUser,
    IAuthUnitOfWork unitOfWork)
    : IRequestHandler<AddMemberCommand, Result<MemberResponse>>
{
    public async Task<Result<MemberResponse>> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(User.NormalizeEmail(request.Email), cancellationToken);
        if (user is null)
            return AuthErrors.UserNotFound(request.Email);

        var workspace = await workspaces.GetWithMembersAsync(currentUser.WorkspaceId, cancellationToken);
        if (workspace is null)
            return AuthErrors.NoWorkspaceAccess;

        var member = workspace.AddMember(user.Id, request.Role);
        if (member.IsFailure)
            return member.Error;

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
            return saved.Error;

        return new MemberResponse(user.Id, user.Email, user.FullName, member.Value.Role, member.Value.IsActive);
    }
}
