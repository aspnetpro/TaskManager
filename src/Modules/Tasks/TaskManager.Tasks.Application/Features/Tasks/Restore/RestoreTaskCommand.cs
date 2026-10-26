namespace TaskManager.Tasks.Application.Features.Tasks.Restore;

public sealed record RestoreTaskCommand(Guid TaskId) : IRequest<Result<TaskResponse>>, IInvalidatesCache
{
    public IEnumerable<string> CacheKeysToInvalidate => [TaskCacheKeys.Details(TaskId)];
}
