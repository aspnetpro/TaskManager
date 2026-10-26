namespace TaskManager.Tasks.Domain.Events;

public sealed record TaskCreatedEvent(Guid TaskId, Guid WorkspaceId, Guid ReporterId) : IDomainEvent;
