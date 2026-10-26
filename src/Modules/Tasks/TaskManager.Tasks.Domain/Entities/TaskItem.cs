namespace TaskManager.Tasks.Domain.Entities;

public sealed class TaskItem : Entity, IAuditable, IVersioned, ISoftDeletable
{
    public const int RestoreWindowDays = 30;
    public const int DueSoonDays = 3;

    private static readonly Dictionary<TaskItemStatus, TaskItemStatus[]> AllowedTransitions = new()
    {
        [TaskItemStatus.Pending] = [TaskItemStatus.InProgress, TaskItemStatus.Cancelled],
        [TaskItemStatus.InProgress] = [TaskItemStatus.Done, TaskItemStatus.Cancelled],
        [TaskItemStatus.Done] = [],
        [TaskItemStatus.Cancelled] = []
    };

    private readonly List<TaskStatusChange> _statusHistory = [];

    private TaskItem()
    {
        // EF Core
    }

    public Guid WorkspaceId { get; private set; }
    public Title Title { get; private set; } = null!;
    public Description? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public Guid ReporterId { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public IReadOnlyList<TaskStatusChange> StatusHistory => _statusHistory;

    public bool IsClosed => Status is TaskItemStatus.Done or TaskItemStatus.Cancelled;

    public static TaskItem Create(
        Guid workspaceId,
        Guid reporterId,
        Title title,
        Description? description,
        TaskPriority priority,
        DateOnly? dueDate)
    {
        var task = new TaskItem
        {
            WorkspaceId = workspaceId,
            ReporterId = reporterId,
            Title = title,
            Description = description,
            Priority = priority,
            DueDate = dueDate,
            Status = TaskItemStatus.Pending
        };

        task.Raise(new TaskCreatedEvent(task.Id, workspaceId, reporterId));
        return task;
    }

    public bool CanBeEditedBy(Guid userId) => userId == ReporterId || userId == AssigneeId;

    public void Rename(Title title) => Title = title;

    public void ChangeDescription(Description? description) => Description = description;

    public void ChangePriority(TaskPriority priority) => Priority = priority;

    public void ChangeDueDate(DateOnly? dueDate) => DueDate = dueDate;

    public Result Start(Guid changedBy, DateTimeOffset now) =>
        TransitionTo(TaskItemStatus.InProgress, changedBy, now);

    public Result Complete(Guid changedBy, DateTimeOffset now) =>
        TransitionTo(TaskItemStatus.Done, changedBy, now);

    public Result Cancel(Guid changedBy, DateTimeOffset now) =>
        TransitionTo(TaskItemStatus.Cancelled, changedBy, now);

    /// <summary>
    /// Única porta de entrada para mudança de status. As regras vivem aqui,
    /// não no handler — nenhum código fora do domínio consegue burlá-las.
    /// </summary>
    public Result TransitionTo(TaskItemStatus newStatus, Guid changedBy, DateTimeOffset now)
    {
        if (!AllowedTransitions[Status].Contains(newStatus))
            return TaskErrors.InvalidTransition(Status, newStatus);

        var previous = Status;
        Status = newStatus;

        // US-005: ao iniciar sem responsável, quem iniciou assume a tarefa.
        if (newStatus == TaskItemStatus.InProgress && AssigneeId is null)
            SetAssignee(changedBy, changedBy);

        if (newStatus == TaskItemStatus.Done)
            CompletedAt = now;

        _statusHistory.Add(new TaskStatusChange(previous, newStatus, changedBy, now));
        Raise(new TaskStatusChangedEvent(Id, previous, newStatus, changedBy));
        return Result.Success();
    }

    public Result AssignTo(Guid? assigneeId, Guid assignedBy)
    {
        if (IsClosed)
            return TaskErrors.ClosedTaskAssignment;

        if (AssigneeId != assigneeId)
            SetAssignee(assigneeId, assignedBy);

        return Result.Success();
    }

    public Result Delete(Guid deletedBy, DateTimeOffset now)
    {
        if (IsDeleted)
            return TaskErrors.AlreadyDeleted;

        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy;
        Raise(new TaskDeletedEvent(Id, deletedBy));
        return Result.Success();
    }

    public Result Restore(DateTimeOffset now)
    {
        if (!IsDeleted)
            return TaskErrors.NotDeleted;

        if (now - DeletedAt > TimeSpan.FromDays(RestoreWindowDays))
            return TaskErrors.RestoreWindowExpired;

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        Raise(new TaskRestoredEvent(Id));
        return Result.Success();
    }

    public bool IsOverdue(DateOnly today) =>
        !IsClosed && DueDate is { } due && due < today;

    public bool IsDueSoon(DateOnly today) =>
        !IsClosed && DueDate is { } due && due >= today && due <= today.AddDays(DueSoonDays);

    private void SetAssignee(Guid? assigneeId, Guid assignedBy)
    {
        var previous = AssigneeId;
        AssigneeId = assigneeId;
        Raise(new TaskAssignedEvent(Id, previous, assigneeId, assignedBy));
    }
}
