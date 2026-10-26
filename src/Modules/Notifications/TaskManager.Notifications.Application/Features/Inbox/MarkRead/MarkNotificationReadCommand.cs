namespace TaskManager.Notifications.Application.Features.Inbox.MarkRead;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;
