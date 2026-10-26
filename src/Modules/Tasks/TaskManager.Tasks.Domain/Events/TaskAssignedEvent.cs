namespace TaskManager.Tasks.Domain.Events;

public sealed record TaskAssignedEvent(
    Guid TaskId,
    Guid? PreviousAssigneeId,
    Guid? AssigneeId,
    Guid AssignedBy) : IDomainEvent;
