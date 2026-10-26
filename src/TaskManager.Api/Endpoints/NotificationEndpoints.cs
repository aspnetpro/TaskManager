using MediatR;
using TaskManager.Api.Http;
using TaskManager.Api.Requests;
using TaskManager.Api.Security;
using TaskManager.Notifications.Application.Features.Inbox;
using TaskManager.Notifications.Application.Features.Inbox.List;
using TaskManager.Notifications.Application.Features.Inbox.MarkRead;
using TaskManager.Shared.Application;

namespace TaskManager.Api.Endpoints;

public static class NotificationEndpoints
{
    private const string BasePath = "/api/v1/notifications";

    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(BasePath)
            .WithTags("Notifications")
            .RequireAuthorization(Policies.WorkspaceMember)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/", ListNotifications)
            .WithName("ListNotifications")
            .Produces<PagedResponse<NotificationResponse>>()
            .ProducesValidationProblem();

        group.MapPost("/{id:guid}/read", MarkRead)
            .WithName("MarkNotificationRead")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListNotifications(
        [AsParameters] ListNotificationsRequest request, ISender sender, CancellationToken ct)
    {
        var defaults = new ListNotificationsQuery();
        var result = await sender.Send(new ListNotificationsQuery(
            request.UnreadOnly ?? defaults.UnreadOnly,
            request.Page ?? defaults.Page,
            request.PageSize ?? defaults.PageSize), ct);

        return result.Match(Results.Ok, error => error.ToProblem());
    }

    private static async Task<IResult> MarkRead(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new MarkNotificationReadCommand(id), ct);
        return result.Match(Results.NoContent, error => error.ToProblem());
    }
}
