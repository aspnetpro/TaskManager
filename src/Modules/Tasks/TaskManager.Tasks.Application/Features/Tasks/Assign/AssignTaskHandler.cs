using TaskManager.Auth.Contracts;

namespace TaskManager.Tasks.Application.Features.Tasks.Assign;

public sealed class AssignTaskHandler(
    ITaskRepository repository,
    ITasksUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IWorkspaceMembershipService membership,
    TimeProvider timeProvider)
    : IRequestHandler<AssignTaskCommand, Result<TaskResponse>>
{
    public async Task<Result<TaskResponse>> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return TaskErrors.NotFound(request.TaskId);

        if (!CanAssign(task, request.AssigneeId))
            return TaskErrors.AssignForbidden;

        // Comunicação entre módulos só via Contracts: Tasks não conhece as entidades do Auth.
        if (request.AssigneeId is { } assigneeId &&
            !await membership.IsActiveMemberAsync(currentUser.WorkspaceId, assigneeId, cancellationToken))
            return TaskErrors.AssigneeNotMember;

        var assigned = task.AssignTo(request.AssigneeId, currentUser.UserId);
        if (assigned.IsFailure)
            return assigned.Error;

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
            return saved.Error;

        return TaskResponse.FromDomain(task, timeProvider.GetUtcToday());
    }

    /// <summary>
    /// US-006: Admin e TechLead atribuem a qualquer membro. Developer só se
    /// auto-atribui ou remove a si mesmo. Viewer é barrado pela policy da Api.
    /// </summary>
    private bool CanAssign(TaskItem task, Guid? assigneeId) => currentUser.Role switch
    {
        WorkspaceRole.Admin or WorkspaceRole.TechLead => true,
        WorkspaceRole.Developer => assigneeId == currentUser.UserId
            || (assigneeId is null && task.AssigneeId == currentUser.UserId),
        _ => false
    };
}
