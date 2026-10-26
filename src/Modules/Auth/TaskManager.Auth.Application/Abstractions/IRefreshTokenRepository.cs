namespace TaskManager.Auth.Application.Abstractions;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);

    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Revoga todos os tokens ativos da família. Executa direto no banco, fora do Unit of Work.</summary>
    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
