namespace TaskManager.Tasks.Application.Features.Tasks;

/// <summary>
/// Detalhe da tarefa (US-003): campos da listagem + histórico de status.
/// Record posicional (construtor único) para ser desserializável por clientes .NET.
/// </summary>
public sealed record TaskDetailsResponse(
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
    bool IsDeleted,
    IReadOnlyList<StatusChangeResponse> StatusHistory)
    : TaskResponse(Id, Title, Description, Status, Priority, ReporterId, AssigneeId, DueDate,
        IsOverdue, IsDueSoon, CompletedAt, CreatedAt, UpdatedAt, Version, IsDeleted)
{
    public static new TaskDetailsResponse FromDomain(TaskItem task, DateOnly today) => new(
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
        task.IsDeleted,
        task.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .Select(h => new StatusChangeResponse(h.From, h.To, h.ChangedBy, h.ChangedAt))
            .ToList());
}
