namespace TaskManager.Tasks.Application.Features.Tasks.Delete;

public sealed record DeleteTaskCommand(Guid TaskId) : IRequest<Result>, IInvalidatesCache
{
    public IEnumerable<string> CacheKeysToInvalidate => [TaskCacheKeys.Details(TaskId)];
}
