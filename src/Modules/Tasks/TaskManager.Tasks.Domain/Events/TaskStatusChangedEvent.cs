namespace TaskManager.Tasks.Domain.Events;

public sealed record TaskStatusChangedEvent(
    Guid TaskId,
    TaskItemStatus From,
    TaskItemStatus To,
    Guid ChangedBy) : IDomainEvent;
