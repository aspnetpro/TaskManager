using MediatR;
using TaskManager.Shared.Application.Caching;

namespace TaskManager.Shared.Application.Behaviors;

/// <summary>
/// Remove as chaves afetadas depois do handler — com sucesso ou não. Invalidar
/// à toa custa um cache miss; deixar de invalidar custa dado velho na tela.
/// </summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse>(IWorkspaceCache cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IInvalidatesCache
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        foreach (var key in request.CacheKeysToInvalidate)
            await cache.RemoveAsync(key, cancellationToken);

        return response;
    }
}
