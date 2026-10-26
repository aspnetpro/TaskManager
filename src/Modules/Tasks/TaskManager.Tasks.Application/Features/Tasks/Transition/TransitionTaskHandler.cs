namespace TaskManager.Tasks.Application.Features.Tasks.Transition;

public sealed class TransitionTaskHandler(
    ITaskRepository repository,
    ITasksUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    TasksMetrics metrics)
    : IRequestHandler<TransitionTaskCommand, Result<TaskResponse>>
{
    public async Task<Result<TaskResponse>> Handle(TransitionTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return TaskErrors.NotFound(request.TaskId);

        // Regra de transição no domínio; o handler só orquestra.
        var transitioned = task.TransitionTo(request.NewStatus, currentUser.UserId, timeProvider.GetUtcNow());
        if (transitioned.IsFailure)
            return transitioned.Error;

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
            return saved.Error;

        if (task.CompletedAt is { } completedAt && request.NewStatus == TaskItemStatus.Done)
            metrics.TaskCompleted(task.Priority, completedAt - task.CreatedAt);

        return TaskResponse.FromDomain(task, timeProvider.GetUtcToday());
    }
}
