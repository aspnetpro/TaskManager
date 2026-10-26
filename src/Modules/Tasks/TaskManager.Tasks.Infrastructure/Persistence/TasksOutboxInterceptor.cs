using TaskManager.Shared.Application.Events;
using TaskManager.Shared.Domain;
using TaskManager.Shared.Infrastructure.Outbox;
using TaskManager.Tasks.Contracts.IntegrationEvents;
using TaskManager.Tasks.Domain.Events;

namespace TaskManager.Tasks.Infrastructure.Persistence;

/// <summary>
/// O que do módulo Tasks vira integration event. Created, Deleted e Restored
/// ficam internos até alguém precisar consumir — contrato público é para sempre.
/// </summary>
internal sealed class TasksOutboxInterceptor(TimeProvider timeProvider, IntegrationEventRegistry registry)
    : OutboxInterceptor(timeProvider, registry)
{
    protected override object? ToIntegrationEvent(Entity source, IDomainEvent domainEvent, DateTimeOffset occurredAt)
    {
        if (source is not TaskItem task)
            return null;

        return domainEvent switch
        {
            TaskAssignedEvent e => new TaskAssignedIntegrationEvent(
                Guid.CreateVersion7(),
                occurredAt,
                task.WorkspaceId,
                task.Id,
                task.Title.Value,
                e.PreviousAssigneeId,
                e.AssigneeId,
                e.AssignedBy),

            TaskStatusChangedEvent e => new TaskStatusChangedIntegrationEvent(
                Guid.CreateVersion7(),
                occurredAt,
                task.WorkspaceId,
                task.Id,
                task.Title.Value,
                e.From.ToString(),
                e.To.ToString(),
                e.ChangedBy,
                task.ReporterId,
                task.AssigneeId),

            _ => null
        };
    }
}
