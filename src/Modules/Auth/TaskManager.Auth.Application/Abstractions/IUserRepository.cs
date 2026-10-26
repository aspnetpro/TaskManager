namespace TaskManager.Auth.Application.Abstractions;

public interface IUserRepository
{
    void Add(User user);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Recebe o email já normalizado (User.NormalizeEmail).</summary>
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
