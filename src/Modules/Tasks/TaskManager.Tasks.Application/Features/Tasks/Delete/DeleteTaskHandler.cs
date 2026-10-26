namespace TaskManager.Tasks.Application.Features.Tasks.Delete;

public sealed class DeleteTaskHandler(
    ITaskRepository repository,
    ITasksUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<DeleteTaskCommand, Result>
{
    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        // A policy AdminOnly já barra na Api; a checagem aqui é defesa em profundidade.
        if (currentUser.Role != WorkspaceRole.Admin)
            return TaskErrors.DeleteForbidden;

        var task = await repository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return TaskErrors.NotFound(request.TaskId);

        var deleted = task.Delete(currentUser.UserId, timeProvider.GetUtcNow());
        if (deleted.IsFailure)
            return deleted.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
