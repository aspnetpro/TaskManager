namespace TaskManager.Notifications.Application.EventHandlers;

/// <summary>Avisa o novo responsável — a menos que ele mesmo tenha se atribuído.</summary>
public sealed class TaskAssignedNotificationHandler(NotificationWriter writer, TimeProvider timeProvider)
    : IIntegrationEventHandler<TaskAssignedIntegrationEvent>
{
    public Task HandleAsync(TaskAssignedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (integrationEvent.AssigneeId is not { } assigneeId || assigneeId == integrationEvent.AssignedBy)
            return Task.CompletedTask;

        var notification = Notification.Create(
            integrationEvent.WorkspaceId,
            assigneeId,
            NotificationType.TaskAssigned,
            integrationEvent.TaskId,
            integrationEvent.Title,
            integrationEvent.AssignedBy,
            detail: null,
            integrationEvent.EventId,
            timeProvider.GetUtcNow());

        return writer.WriteAsync([notification], cancellationToken);
    }
}
