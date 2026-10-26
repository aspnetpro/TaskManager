namespace TaskManager.Auth.Application.Features.Auth.Register;

public sealed class RegisterHandler(
    IUserRepository users,
    IWorkspaceRepository workspaces,
    IPasswordHasher passwordHasher,
    SessionIssuer sessions,
    IAuthUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCommand, Result<AuthResult>>
{
    public async Task<Result<AuthResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        if (await users.EmailExistsAsync(email, cancellationToken))
            return AuthErrors.EmailAlreadyRegistered;

        var user = User.Create(email, request.FullName, passwordHasher.Hash(request.Password));
        var workspace = Workspace.Create(request.WorkspaceName);
        var member = workspace.AddMember(user.Id, WorkspaceRole.Admin);
        if (member.IsFailure)
            return member.Error;

        users.Add(user);
        workspaces.Add(workspace);
        var session = sessions.Start(user, new MembershipSummary(workspace.Id, workspace.Name, WorkspaceRole.Admin));

        // Corrida entre dois registros com o mesmo email: o índice único barra o segundo (409).
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
            return saved.Error.Type == ErrorType.Conflict ? AuthErrors.EmailAlreadyRegistered : saved.Error;

        return session;
    }
}
