using Microsoft.EntityFrameworkCore;
using TaskManager.Notifications.Application.Abstractions;
using TaskManager.Notifications.Domain;

namespace TaskManager.Notifications.Infrastructure.Persistence;

internal sealed class NotificationRepository(NotificationsDbContext context) : INotificationRepository
{
    public void AddRange(IEnumerable<Notification> notifications) => context.Notifications.AddRange(notifications);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> ListAsync(
        bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Notifications.AsNoTracking();
        if (unreadOnly)
            query = query.Where(n => n.ReadAt == null);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
