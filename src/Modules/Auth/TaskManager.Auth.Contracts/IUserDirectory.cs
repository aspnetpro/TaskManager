namespace TaskManager.Auth.Contracts;

/// <summary>Dados de contato de usuários para outros módulos (ex: email do Notifications).</summary>
public interface IUserDirectory
{
    Task<UserContact?> GetContactAsync(Guid userId, CancellationToken cancellationToken = default);
}
