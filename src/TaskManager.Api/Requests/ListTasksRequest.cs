using TaskManager.Tasks.Application.Abstractions;
using TaskManager.Tasks.Domain.Enums;

namespace TaskManager.Api.Requests;

public sealed record ListTasksRequest(
    int? Page,
    int? PageSize,
    TaskItemStatus? Status,
    TaskPriority? Priority,
    Guid? AssigneeId,
    string? Search,
    bool? Overdue,
    bool? IncludeDeleted,
    TaskSortField? SortBy,
    SortDirection? SortDirection);
