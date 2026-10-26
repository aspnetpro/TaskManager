using Microsoft.EntityFrameworkCore;
using TaskManager.Auth.Application.Abstractions;
using TaskManager.Auth.Domain.Entities;

namespace TaskManager.Auth.Infrastructure.Persistence;

internal sealed class RefreshTokenRepository(AuthDbContext context) : IRefreshTokenRepository
{
    public void Add(RefreshToken token) => context.RefreshTokens.Add(token);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        context.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
}
