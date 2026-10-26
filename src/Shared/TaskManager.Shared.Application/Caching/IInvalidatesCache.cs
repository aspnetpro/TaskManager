namespace TaskManager.Shared.Application.Caching;

/// <summary>Command que invalida chaves do IWorkspaceCache depois de executar.</summary>
public interface IInvalidatesCache
{
    IEnumerable<string> CacheKeysToInvalidate { get; }
}
