namespace TaskManager.Auth.Application.Features.Auth;

/// <summary>
/// Resultado de login/registro/refresh. A Api separa o refresh token (cookie
/// httpOnly) do resto (body) — ele nunca chega ao JavaScript do frontend.
/// </summary>
public sealed record AuthResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    SessionUser User,
    MembershipSummary Workspace);
