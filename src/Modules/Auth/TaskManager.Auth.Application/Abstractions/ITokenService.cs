namespace TaskManager.Auth.Application.Abstractions;

public interface ITokenService
{
    TimeSpan RefreshTokenLifetime { get; }

    AccessToken CreateAccessToken(User user, MembershipSummary membership, DateTimeOffset now);

    /// <summary>Token aleatório (vai para o cookie) e o hash dele (vai para o banco).</summary>
    GeneratedRefreshToken GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
