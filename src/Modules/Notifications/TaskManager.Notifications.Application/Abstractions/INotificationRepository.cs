namespace TaskManager.Notifications.Application.Abstractions;

public interface INotificationRepository
{
    void AddRange(IEnumerable<Notification> notifications);

    /// <summary>Filtrado pelo workspace e pelo destinatário do usuário atual.</summary>
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Notification> Items, int TotalCount)> ListAsync(
        bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default);
}
