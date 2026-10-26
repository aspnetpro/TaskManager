namespace TaskManager.Tasks.Application.Features.Tasks.Update;

/// <summary>
/// PATCH semântico: null = campo não enviado. Description vazia limpa o campo;
/// ClearDueDate remove a data limite. ExpectedVersion vem do header If-Match.
/// </summary>
public sealed record UpdateTaskCommand(
    Guid TaskId,
    uint ExpectedVersion,
    string? Title = null,
    string? Description = null,
    TaskPriority? Priority = null,
    DateOnly? DueDate = null,
    bool ClearDueDate = false) : IRequest<Result<TaskResponse>>, IInvalidatesCache
{
    public IEnumerable<string> CacheKeysToInvalidate => [TaskCacheKeys.Details(TaskId)];
}
