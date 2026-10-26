namespace TaskManager.Notifications.Application.Features.Inbox.List;

public sealed class ListNotificationsQueryValidator : AbstractValidator<ListNotificationsQuery>
{
    public const int MaxPageSize = 100;

    public ListNotificationsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}
