namespace TaskManager.Tasks.Application.Features.Tasks.Create;

public sealed class CreateTaskHandler(
    ITaskRepository repository,
    ITasksUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    TasksMetrics metrics)
    : IRequestHandler<CreateTaskCommand, Result<TaskResponse>>
{
    public async Task<Result<TaskResponse>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var title = Title.Create(request.Title);
        if (title.IsFailure)
            return title.Error;

        var description = Description.Create(request.Description);
        if (description.IsFailure)
            return description.Error;

        var task = TaskItem.Create(
            currentUser.WorkspaceId,
            currentUser.UserId,
            title.Value,
            description.Value,
            request.Priority,
            request.DueDate);

        repository.Add(task);

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
            return saved.Error;

        metrics.TaskCreated(task.Priority);
        return TaskResponse.FromDomain(task, timeProvider.GetUtcToday());
    }
}
