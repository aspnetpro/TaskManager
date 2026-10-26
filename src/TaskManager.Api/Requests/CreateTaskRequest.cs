using TaskManager.Tasks.Application.Abstractions;
using TaskManager.Tasks.Domain.Enums;

namespace TaskManager.Api.Requests;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate);
