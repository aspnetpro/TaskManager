namespace TaskManager.Tasks.Application.Features.Tasks.Restore;

public sealed class RestoreTaskHandler(
    ITaskRepository repository,
    ITasksUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<RestoreTaskCommand, Result<TaskResponse>>
{
    public async Task<Result<TaskResponse>> Handle(RestoreTaskCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != WorkspaceRole.Admin)
            return TaskErrors.DeleteForbidden;

        var task = await repository.GetDeletedByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return TaskErrors.NotFound(request.TaskId);

        var restored = task.Restore(timeProvider.GetUtcNow());
        if (restored.IsFailure)
            return restored.Error;

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
            return saved.Error;

        return TaskResponse.FromDomain(task, timeProvider.GetUtcToday());
    }
}
