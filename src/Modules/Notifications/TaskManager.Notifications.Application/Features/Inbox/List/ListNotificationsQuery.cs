
namespace TaskManager.Notifications.Application.Features.Inbox.List;

public sealed record ListNotificationsQuery(bool UnreadOnly = false, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResponse<NotificationResponse>>>;
