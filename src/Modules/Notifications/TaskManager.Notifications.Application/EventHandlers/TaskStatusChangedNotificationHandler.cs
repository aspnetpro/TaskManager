namespace TaskManager.Notifications.Application.EventHandlers;

/// <summary>Avisa reporter e responsável sobre a mudança de status, exceto quem mudou.</summary>
public sealed class TaskStatusChangedNotificationHandler(NotificationWriter writer, TimeProvider timeProvider)
    : IIntegrationEventHandler<TaskStatusChangedIntegrationEvent>
{
    public Task HandleAsync(TaskStatusChangedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var recipients = new HashSet<Guid> { integrationEvent.ReporterId };
        if (integrationEvent.AssigneeId is { } assigneeId)
            recipients.Add(assigneeId);
        recipients.Remove(integrationEvent.ChangedBy);

        var notifications = recipients
            .Select(recipient => Notification.Create(
                integrationEvent.WorkspaceId,
                recipient,
                NotificationType.TaskStatusChanged,
                integrationEvent.TaskId,
                integrationEvent.Title,
                integrationEvent.ChangedBy,
                integrationEvent.ToStatus,
                integrationEvent.EventId,
                now))
            .ToList();

        return writer.WriteAsync(notifications, cancellationToken);
    }
}
