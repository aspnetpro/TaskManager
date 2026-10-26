
namespace TaskManager.Notifications.Application.Features.Inbox.List;

public sealed class ListNotificationsHandler(INotificationRepository repository)
    : IRequestHandler<ListNotificationsQuery, Result<PagedResponse<NotificationResponse>>>
{
    public async Task<Result<PagedResponse<NotificationResponse>>> Handle(
        ListNotificationsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.ListAsync(request.UnreadOnly, request.Page, request.PageSize, cancellationToken);

        return new PagedResponse<NotificationResponse>(
            items.Select(NotificationResponse.FromDomain).ToList(),
            total,
            request.Page,
            request.PageSize);
    }
}
