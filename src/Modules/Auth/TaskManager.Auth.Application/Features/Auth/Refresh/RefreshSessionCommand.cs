namespace TaskManager.Auth.Application.Features.Auth.Refresh;

/// <summary>O refresh token vem do cookie httpOnly, lido pela Api.</summary>
public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<Result<AuthResult>>;
