using TaskManager.Shared.Domain;

namespace TaskManager.Auth.Domain.Entities;

/// <summary>
/// Conta de usuário. Não herda de IdentityUser: o domínio do Auth é nosso, e
/// o hash de senha chega pronto da Infrastructure (PBKDF2 do PasswordHasher).
/// </summary>
public sealed class User : Entity, IAuditable
{
    public const int MaxEmailLength = 256;
    public const int MaxFullNameLength = 100;

    private User()
    {
        // EF Core
    }

    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static User Create(string email, string fullName, string passwordHash, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Id = id ?? Guid.CreateVersion7(),
            Email = NormalizeEmail(email),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash
        };
    }

    /// <summary>Chamado quando o hasher pede rehash (ex: aumento de iterações do PBKDF2).</summary>
    public void ChangePasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    /// <summary>Email é comparado sempre normalizado: "Ana@X.com" e "ana@x.com" são a mesma conta.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
