namespace TaskManager.Tasks.Application.Features.Tasks.List;

public sealed class GetTasksHandler(
    ITaskRepository repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<GetTasksQuery, Result<PagedResponse<TaskResponse>>>
{
    public async Task<Result<PagedResponse<TaskResponse>>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        // US-007: só Admin enxerga tarefas excluídas.
        if (request.IncludeDeleted && currentUser.Role != WorkspaceRole.Admin)
            return TaskErrors.ViewDeletedForbidden;

        var today = timeProvider.GetUtcToday();

        var filter = new TaskListFilter(
            request.Page,
            request.PageSize,
            request.Status,
            request.Priority,
            request.AssigneeId,
            string.IsNullOrWhiteSpace(request.SearchTerm) ? null : request.SearchTerm.Trim(),
            request.Overdue ? today : null,
            request.IncludeDeleted,
            request.SortBy,
            request.SortDirection);

        var (items, totalCount) = await repository.ListAsync(filter, cancellationToken);

        return new PagedResponse<TaskResponse>(
            items.Select(t => TaskResponse.FromDomain(t, today)).ToList(),
            totalCount,
            request.Page,
            request.PageSize);
    }
}
