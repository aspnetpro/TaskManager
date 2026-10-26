using TaskManager.Tasks.Application.Abstractions;
using TaskManager.Tasks.Domain.Enums;

namespace TaskManager.Api.Requests;

public sealed record UpdateTaskRequest(
    string? Title,
    string? Description,
    TaskPriority? Priority,
    DateOnly? DueDate,
    bool ClearDueDate = false);
