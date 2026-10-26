namespace TaskManager.Tasks.Domain.Events;

public sealed record TaskDeletedEvent(Guid TaskId, Guid DeletedBy) : IDomainEvent;
