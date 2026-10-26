using TaskManager.Shared.Domain;

namespace TaskManager.Auth.Domain.Entities;

/// <summary>
/// Refresh token com rotação: cada uso gera um novo token da mesma família e
/// revoga o anterior. Apresentar um token já revogado é sinal de roubo — a
/// família inteira cai. Só o hash SHA-256 é persistido, nunca o token.
/// </summary>
public sealed class RefreshToken : Entity
{
    public const int TokenHashLength = 64;

    private RefreshToken()
    {
        // EF Core
    }

    public Guid UserId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public Guid FamilyId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;

    /// <summary>Primeiro token de uma sessão (login/registro): abre uma família nova.</summary>
    public static RefreshToken StartFamily(
        Guid userId, Guid workspaceId, string tokenHash, DateTimeOffset now, TimeSpan lifetime) =>
        Create(userId, workspaceId, tokenHash, Guid.CreateVersion7(), now, lifetime);

    /// <summary>Troca este token por um novo da mesma família.</summary>
    public RefreshToken Rotate(string newTokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        if (!IsActive(now))
            throw new InvalidOperationException("Only an active refresh token can be rotated.");

        var next = Create(UserId, WorkspaceId, newTokenHash, FamilyId, now, lifetime);
        RevokedAt = now;
        ReplacedByTokenId = next.Id;
        return next;
    }

    private static RefreshToken Create(
        Guid userId, Guid workspaceId, string tokenHash, Guid familyId, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new RefreshToken
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime)
        };
    }
}
