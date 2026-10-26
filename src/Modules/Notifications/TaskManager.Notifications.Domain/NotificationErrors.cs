using TaskManager.Shared.Domain.Results;

namespace TaskManager.Notifications.Domain;

public static class NotificationErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Notification.NotFound", $"Notification '{id}' was not found.");
}
