namespace TaskManager.Auth.Application.Features.Auth.Login;

public sealed class LoginHandler(
    IUserRepository users,
    IWorkspaceRepository workspaces,
    IPasswordHasher passwordHasher,
    SessionIssuer sessions,
    IAuthUnitOfWork unitOfWork)
    : IRequestHandler<LoginCommand, Result<AuthResult>>
{
    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(User.NormalizeEmail(request.Email), cancellationToken);

        // Verifica a senha mesmo sem usuário: mesma resposta e mesmo custo nos dois casos.
        var verification = passwordHasher.Verify(user?.PasswordHash, request.Password);
        if (user is null || verification == PasswordVerification.Failed)
            return AuthErrors.InvalidCredentials;

        if (verification == PasswordVerification.SuccessRehashNeeded)
            user.ChangePasswordHash(passwordHasher.Hash(request.Password));

        var memberships = await workspaces.GetActiveMembershipsAsync(user.Id, cancellationToken);
        var membership = request.WorkspaceId is { } workspaceId
            ? memberships.FirstOrDefault(m => m.WorkspaceId == workspaceId)
            : memberships.FirstOrDefault();

        if (membership is null)
            return AuthErrors.NoWorkspaceAccess;

        var session = sessions.Start(user, membership);

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
            return saved.Error;

        return session;
    }
}
