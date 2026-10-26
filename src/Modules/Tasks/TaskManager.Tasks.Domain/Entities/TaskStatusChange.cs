namespace TaskManager.Tasks.Domain.Entities;

/// <summary>Entrada do histórico de status (US-003). Owned entity do TaskItem.</summary>
public sealed class TaskStatusChange
{
    private TaskStatusChange()
    {
        // EF Core
    }

    internal TaskStatusChange(TaskItemStatus from, TaskItemStatus to, Guid changedBy, DateTimeOffset changedAt)
    {
        From = from;
        To = to;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
    }

    public TaskItemStatus From { get; private set; }
    public TaskItemStatus To { get; private set; }
    public Guid ChangedBy { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
}
