using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Auth.Application.Abstractions;
using TaskManager.Auth.Contracts;
using TaskManager.Auth.Domain.Entities;

namespace TaskManager.Auth.Infrastructure.Security;

internal sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private const int RefreshTokenBytes = 32;

    private static readonly JsonWebTokenHandler Handler = new();

    public TimeSpan RefreshTokenLifetime => options.Value.RefreshTokenLifetime;

    public AccessToken CreateAccessToken(User user, MembershipSummary membership, DateTimeOffset now)
    {
        var settings = options.Value;
        var expiresAt = now.Add(settings.AccessTokenLifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [WorkspaceClaimTypes.UserId] = user.Id.ToString(),
                [WorkspaceClaimTypes.Email] = user.Email,
                [WorkspaceClaimTypes.Name] = user.FullName,
                [WorkspaceClaimTypes.WorkspaceId] = membership.WorkspaceId.ToString(),
                [WorkspaceClaimTypes.WorkspaceRole] = membership.Role.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString()
            }
        };

        return new AccessToken(Handler.CreateToken(descriptor), expiresAt);
    }

    public GeneratedRefreshToken GenerateRefreshToken()
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        return new GeneratedRefreshToken(value, HashRefreshToken(value));
    }

    /// <summary>
    /// SHA-256 basta: o token tem 256 bits aleatórios, não há dicionário para
    /// atacar. PBKDF2 aqui só custaria CPU a cada refresh.
    /// </summary>
    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
