using TaskManager.Shared.Domain.Results;

namespace TaskManager.Shared.Application.Caching;

/// <summary>
/// Cache com escopo de workspace. A chave recebe o prefixo do tenant aqui,
/// não em cada query: uma chave "task:{id}" sem tenant devolveria a tarefa de
/// outro workspace sem passar pelo query filter — IDOR via cache.
/// Só resultados de sucesso são cacheados. T é sempre referência: "null" é
/// o sinal de cache miss (com int, o default 0 viraria um hit eterno).
/// </summary>
public interface IWorkspaceCache
{
    Task<Result<T>> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<Result<T>>> factory,
        CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
