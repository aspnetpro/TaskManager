namespace TaskManager.Tasks.Application.Features.Tasks.Transition;

public sealed record TransitionTaskCommand(Guid TaskId, TaskItemStatus NewStatus) : IRequest<Result<TaskResponse>>, IInvalidatesCache
{
    public IEnumerable<string> CacheKeysToInvalidate => [TaskCacheKeys.Details(TaskId)];
}
