namespace TaskManager.Auth.Application.Features.Auth.Logout;

/// <summary>Idempotente: token desconhecido ou já revogado também é sucesso.</summary>
public sealed class LogoutHandler(IRefreshTokenRepository refreshTokens, ITokenService tokens, TimeProvider timeProvider)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var current = await refreshTokens.GetByHashAsync(tokens.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (current is not null)
            await refreshTokens.RevokeFamilyAsync(current.FamilyId, timeProvider.GetUtcNow(), cancellationToken);

        return Result.Success();
    }
}
