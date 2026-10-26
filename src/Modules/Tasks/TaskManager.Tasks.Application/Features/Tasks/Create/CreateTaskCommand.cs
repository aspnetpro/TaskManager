namespace TaskManager.Tasks.Application.Features.Tasks.Create;

public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate = null) : IRequest<Result<TaskResponse>>;
