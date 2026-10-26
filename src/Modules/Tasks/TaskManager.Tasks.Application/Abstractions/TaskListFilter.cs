namespace TaskManager.Tasks.Application.Abstractions;

public sealed record TaskListFilter(
    int Page,
    int PageSize,
    TaskItemStatus? Status,
    TaskPriority? Priority,
    Guid? AssigneeId,
    string? SearchTerm,
    DateOnly? OverdueOn,
    bool IncludeDeleted,
    TaskSortField SortBy,
    SortDirection SortDirection);
