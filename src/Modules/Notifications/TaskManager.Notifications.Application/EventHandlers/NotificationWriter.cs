namespace TaskManager.Notifications.Application.EventHandlers;

/// <summary>
/// Grava notificações de um evento. Duplicate key = evento já processado
/// (o Outbox entrega at-least-once) e é tratado como sucesso. Qualquer outra
/// falha vira exceção: é o sinal para o Outbox tentar de novo.
/// </summary>
public sealed class NotificationWriter(INotificationRepository repository, INotificationsUnitOfWork unitOfWork)
{
    public async Task WriteAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken)
    {
        if (notifications.Count == 0)
            return;

        repository.AddRange(notifications);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (saved.IsFailure && saved.Error != CommonErrors.DuplicateKey)
            throw new InvalidOperationException($"Could not save notifications: {saved.Error.Code}");
    }
}
