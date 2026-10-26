using TaskManager.Notifications.Application.Abstractions;
using TaskManager.Shared.Infrastructure.Persistence;

namespace TaskManager.Notifications.Infrastructure.Persistence;

internal sealed class NotificationsUnitOfWork(NotificationsDbContext context)
    : EfUnitOfWork<NotificationsDbContext>(context), INotificationsUnitOfWork;
