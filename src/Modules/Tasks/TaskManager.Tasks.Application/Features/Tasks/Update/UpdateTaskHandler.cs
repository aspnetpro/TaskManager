namespace TaskManager.Tasks.Application.Features.Tasks.Update;

public sealed class UpdateTaskHandler(
    ITaskRepository repository,
    ITasksUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<UpdateTaskCommand, Result<TaskResponse>>
{
    public async Task<Result<TaskResponse>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return TaskErrors.NotFound(request.TaskId);

        // US-004: apenas reporter ou assignee editam.
        if (!task.CanBeEditedBy(currentUser.UserId))
            return TaskErrors.EditForbidden;

        // Falha rápida para o caso comum. A corrida entre este ponto e o
        // SaveChanges é coberta pelo concurrency token no EF Core.
        if (task.Version != request.ExpectedVersion)
            return CommonErrors.ConcurrencyConflict;

        var applied = Apply(task, request);
        if (applied.IsFailure)
            return applied.Error;

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
            return saved.Error;

        return TaskResponse.FromDomain(task, timeProvider.GetUtcToday());
    }

    private static Result Apply(TaskItem task, UpdateTaskCommand request)
    {
        if (request.Title is not null)
        {
            var title = Title.Create(request.Title);
            if (title.IsFailure)
                return title.Error;
            task.Rename(title.Value);
        }

        if (request.Description is not null)
        {
            var description = Description.Create(request.Description);
            if (description.IsFailure)
                return description.Error;
            task.ChangeDescription(description.Value);
        }

        if (request.Priority is { } priority)
            task.ChangePriority(priority);

        if (request.ClearDueDate)
            task.ChangeDueDate(null);
        else if (request.DueDate is { } dueDate)
            task.ChangeDueDate(dueDate);

        return Result.Success();
    }
}
