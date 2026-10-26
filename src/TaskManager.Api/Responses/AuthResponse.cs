using TaskManager.Auth.Application.Abstractions;
using TaskManager.Auth.Application.Features.Auth;

namespace TaskManager.Api.Responses;

/// <summary>Body de login/registro/refresh. O refresh token vai no cookie, nunca aqui.</summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    SessionUser User,
    MembershipSummary Workspace)
{
    public static AuthResponse From(AuthResult result) =>
        new(result.AccessToken, result.AccessTokenExpiresAt, result.User, result.Workspace);
}
