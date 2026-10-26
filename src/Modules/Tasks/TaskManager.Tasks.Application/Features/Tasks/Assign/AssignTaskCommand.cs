namespace TaskManager.Tasks.Application.Features.Tasks.Assign;

/// <summary>AssigneeId null remove o responsável.</summary>
public sealed record AssignTaskCommand(Guid TaskId, Guid? AssigneeId) : IRequest<Result<TaskResponse>>, IInvalidatesCache
{
    public IEnumerable<string> CacheKeysToInvalidate => [TaskCacheKeys.Details(TaskId)];
}
