using Microsoft.AspNetCore.Identity;
using TaskManager.Auth.Application.Abstractions;
using TaskManager.Auth.Domain.Entities;

namespace TaskManager.Auth.Infrastructure.Security;

/// <summary>
/// PBKDF2 (HMAC-SHA512, 100k iterações) do Identity, sem as stores EF do
/// Identity. Quando a Microsoft subir as iterações, o Verify devolve
/// SuccessRehashNeeded e o login regrava o hash.
/// </summary>
internal sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<User> Hasher = new();

    // O parâmetro user não é usado pelo PasswordHasher padrão.
    private static readonly Lazy<string> DummyHash = new(() => Hasher.HashPassword(null!, Guid.NewGuid().ToString()));

    public string Hash(string password) => Hasher.HashPassword(null!, password);

    public PasswordVerification Verify(string? passwordHash, string password)
    {
        if (passwordHash is null)
        {
            Hasher.VerifyHashedPassword(null!, DummyHash.Value, password);
            return PasswordVerification.Failed;
        }

        return Hasher.VerifyHashedPassword(null!, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
            _ => PasswordVerification.Failed
        };
    }
}
