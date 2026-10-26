namespace TaskManager.Tasks.Application.Features.Tasks.GetById;

public sealed class GetTaskByIdHandler(ITaskRepository repository, IWorkspaceCache cache, TimeProvider timeProvider)
    : IRequestHandler<GetTaskByIdQuery, Result<TaskDetailsResponse>>
{
    public Task<Result<TaskDetailsResponse>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken) =>
        // Cache-aside com escopo de workspace; os commands que mudam a tarefa invalidam a chave.
        cache.GetOrCreateAsync(
            TaskCacheKeys.Details(request.TaskId),
            ct => LoadAsync(request.TaskId, ct),
            cancellationToken);

    private async Task<Result<TaskDetailsResponse>> LoadAsync(Guid taskId, CancellationToken cancellationToken)
    {
        // Tarefa de outro workspace volta null pelo query filter: 404, não 403,
        // para não revelar que o Id existe.
        var task = await repository.GetForReadAsync(taskId, cancellationToken);
        if (task is null)
            return TaskErrors.NotFound(taskId);

        return TaskDetailsResponse.FromDomain(task, timeProvider.GetUtcToday());
    }
}
