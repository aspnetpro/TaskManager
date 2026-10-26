namespace TaskManager.Tasks.UnitTests.Common;

/// <summary>Test Data Builder (Cap. 11): só declara o que importa para o teste.</summary>
public sealed class TaskItemBuilder
{
    private readonly Guid _workspaceId = TestUsers.WorkspaceId;
    private Guid _reporterId = TestUsers.ReporterId;
    private string _title = "Implement auth";
    private readonly string? _description = "JWT-based";
    private readonly TaskPriority _priority = TaskPriority.Medium;
    private DateOnly? _dueDate;
    private TaskItemStatus _status = TaskItemStatus.Pending;
    private Guid? _assigneeId;

    public TaskItemBuilder WithReporter(Guid reporterId)
    {
        _reporterId = reporterId;
        return this;
    }

    public TaskItemBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public TaskItemBuilder WithDueDate(DateOnly? dueDate)
    {
        _dueDate = dueDate;
        return this;
    }

    public TaskItemBuilder WithAssignee(Guid? assigneeId)
    {
        _assigneeId = assigneeId;
        return this;
    }

    public TaskItemBuilder InStatus(TaskItemStatus status)
    {
        _status = status;
        return this;
    }

    public TaskItem Build()
    {
        var task = TaskItem.Create(
            _workspaceId,
            _reporterId,
            Title.Create(_title).Value,
            Description.Create(_description).Value,
            _priority,
            _dueDate);

        if (_assigneeId is { } assigneeId)
            task.AssignTo(assigneeId, _reporterId);

        var now = DateTimeOffset.UtcNow;
        var actor = _assigneeId ?? _reporterId;
        switch (_status)
        {
            case TaskItemStatus.InProgress:
                task.Start(actor, now);
                break;
            case TaskItemStatus.Done:
                task.Start(actor, now);
                task.Complete(actor, now);
                break;
            case TaskItemStatus.Cancelled:
                task.Cancel(actor, now);
                break;
        }

        task.ClearDomainEvents();
        return task;
    }
}
