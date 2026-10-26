namespace TaskManager.Auth.Application.Features.Auth;

/// <summary>Emite o par access + refresh token. Usado por registro, login e refresh.</summary>
public sealed class SessionIssuer(ITokenService tokens, IRefreshTokenRepository refreshTokens, TimeProvider timeProvider)
{
    public AuthResult Start(User user, MembershipSummary membership)
    {
        var now = timeProvider.GetUtcNow();
        var refresh = tokens.GenerateRefreshToken();
        var stored = RefreshToken.StartFamily(user.Id, membership.WorkspaceId, refresh.Hash, now, tokens.RefreshTokenLifetime);
        refreshTokens.Add(stored);

        return Build(user, membership, refresh.Value, stored, now);
    }

    public AuthResult Rotate(User user, MembershipSummary membership, RefreshToken current)
    {
        var now = timeProvider.GetUtcNow();
        var refresh = tokens.GenerateRefreshToken();
        var next = current.Rotate(refresh.Hash, now, tokens.RefreshTokenLifetime);
        refreshTokens.Add(next);

        return Build(user, membership, refresh.Value, next, now);
    }

    private AuthResult Build(User user, MembershipSummary membership, string refreshValue, RefreshToken stored, DateTimeOffset now)
    {
        var access = tokens.CreateAccessToken(user, membership, now);
        return new AuthResult(
            access.Value,
            access.ExpiresAt,
            refreshValue,
            stored.ExpiresAt,
            new SessionUser(user.Id, user.Email, user.FullName),
            membership);
    }
}
