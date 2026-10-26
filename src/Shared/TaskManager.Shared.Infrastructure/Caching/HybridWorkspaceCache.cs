using Microsoft.Extensions.Caching.Hybrid;
using TaskManager.Shared.Application;
using TaskManager.Shared.Application.Caching;
using TaskManager.Shared.Domain.Results;

namespace TaskManager.Shared.Infrastructure.Caching;

/// <summary>
/// HybridCache com escopo de workspace. Falhas não são cacheadas — por isso leitura
/// e escrita separadas, e não um GetOrCreateAsync que guardaria o NotFound também.
/// Com Redis (várias réplicas), o L1 em memória fica desligado: RemoveAsync não limpa
/// o L1 das outras réplicas, e um L1 velho devolve a Version antiga — ETag velho,
/// 409 na próxima edição do próprio usuário. Sem Redis (uma instância), o L1 é o cache.
/// </summary>
internal sealed class HybridWorkspaceCache(HybridCache cache, ICurrentUser currentUser, bool useLocalCache)
    : IWorkspaceCache
{
    private const HybridCacheEntryFlags NoWrite =
        HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite;

    private readonly HybridCacheEntryOptions _read = new()
    {
        Flags = useLocalCache ? NoWrite : NoWrite | HybridCacheEntryFlags.DisableLocalCache
    };

    private readonly HybridCacheEntryOptions? _write = useLocalCache
        ? null
        : new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableLocalCache };

    public async Task<Result<T>> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<Result<T>>> factory,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var scopedKey = Scoped(key);

        var cached = await cache.GetOrCreateAsync<T?>(
            scopedKey,
            static _ => ValueTask.FromResult<T?>(null),
            _read,
            cancellationToken: cancellationToken);

        if (cached is not null)
            return cached;

        var result = await factory(cancellationToken);
        if (result.IsSuccess)
            await cache.SetAsync(scopedKey, result.Value, _write, cancellationToken: cancellationToken);

        return result;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(Scoped(key), cancellationToken).AsTask();

    private string Scoped(string key) => $"ws:{currentUser.WorkspaceId:N}:{key}";
}
