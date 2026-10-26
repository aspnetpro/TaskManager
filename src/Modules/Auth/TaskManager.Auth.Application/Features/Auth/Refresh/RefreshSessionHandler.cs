namespace TaskManager.Auth.Application.Features.Auth.Refresh;

public sealed class RefreshSessionHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IWorkspaceRepository workspaces,
    ITokenService tokens,
    SessionIssuer sessions,
    IAuthUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<RefreshSessionCommand, Result<AuthResult>>
{
    public async Task<Result<AuthResult>> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var current = await refreshTokens.GetByHashAsync(tokens.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (current is null)
            return AuthErrors.InvalidRefreshToken;

        // Token já trocado voltando = alguém tem uma cópia. Derruba a sessão inteira.
        if (current.IsRevoked)
        {
            await refreshTokens.RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
            return AuthErrors.InvalidRefreshToken;
        }

        if (!current.IsActive(now))
            return AuthErrors.InvalidRefreshToken;

        // Role pode ter mudado e o membro pode ter sido desativado desde o login: relê sempre.
        var memberships = await workspaces.GetActiveMembershipsAsync(current.UserId, cancellationToken);
        var membership = memberships.FirstOrDefault(m => m.WorkspaceId == current.WorkspaceId);
        var user = await users.GetByIdAsync(current.UserId, cancellationToken);
        if (membership is null || user is null)
        {
            await refreshTokens.RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
            return AuthErrors.InvalidRefreshToken;
        }

        var session = sessions.Rotate(user, membership, current);

        // Dois refreshes simultâneos com o mesmo token: RevokedAt é concurrency token, o segundo perde.
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
            return saved.Error.Type == ErrorType.Conflict ? AuthErrors.InvalidRefreshToken : saved.Error;

        return session;
    }
}
