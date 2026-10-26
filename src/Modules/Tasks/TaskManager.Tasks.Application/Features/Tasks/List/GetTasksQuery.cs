namespace TaskManager.Tasks.Application.Features.Tasks.List;

public sealed record GetTasksQuery(
    int Page = 1,
    int PageSize = 20,
    TaskItemStatus? Status = null,
    TaskPriority? Priority = null,
    Guid? AssigneeId = null,
    string? SearchTerm = null,
    bool Overdue = false,
    bool IncludeDeleted = false,
    TaskSortField SortBy = TaskSortField.CreatedAt,
    SortDirection SortDirection = SortDirection.Desc) : IRequest<Result<PagedResponse<TaskResponse>>>
{
    public const int MaxPageSize = 100;
    public const int MaxSearchTermLength = 200;
}
