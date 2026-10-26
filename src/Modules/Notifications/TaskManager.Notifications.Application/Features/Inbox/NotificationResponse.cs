namespace TaskManager.Notifications.Application.Features.Inbox;

public sealed record NotificationResponse(
    Guid Id,
    NotificationType Type,
    Guid TaskId,
    string TaskTitle,
    Guid ActorId,
    string? Detail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt)
{
    public static NotificationResponse FromDomain(Notification n) =>
        new(n.Id, n.Type, n.TaskId, n.TaskTitle, n.ActorId, n.Detail, n.CreatedAt, n.ReadAt);
}
