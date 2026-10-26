namespace TaskManager.Tasks.Application.Features.Tasks;

/// <summary>
/// Response do slice. Fica na Application (não em Contracts): Contracts é
/// a API pública para outros módulos e não pode depender do Domain.
/// </summary>
public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    Guid ReporterId,
    Guid? AssigneeId,
    DateOnly? DueDate,
    bool IsOverdue,
    bool IsDueSoon,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    uint Version,
    bool IsDeleted)
{
    public static TaskResponse FromDomain(TaskItem task, DateOnly today) => new(
        task.Id,
        task.Title.Value,
        task.Description?.Value,
        task.Status,
        task.Priority,
        task.ReporterId,
        task.AssigneeId,
        task.DueDate,
        task.IsOverdue(today),
        task.IsDueSoon(today),
        task.CompletedAt,
        task.CreatedAt,
        task.UpdatedAt,
        task.Version,
        task.IsDeleted);
}
