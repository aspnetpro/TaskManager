namespace TaskManager.Notifications.Application.Features.Inbox.MarkRead;

public sealed class MarkNotificationReadHandler(
    INotificationRepository repository,
    INotificationsUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        // Notificação de outro usuário volta null pelo query filter: 404.
        var notification = await repository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
            return NotificationErrors.NotFound(request.NotificationId);

        notification.MarkRead(timeProvider.GetUtcNow());
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
