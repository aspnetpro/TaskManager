namespace TaskManager.Tasks.Domain.Events;

public sealed record TaskRestoredEvent(Guid TaskId) : IDomainEvent;
