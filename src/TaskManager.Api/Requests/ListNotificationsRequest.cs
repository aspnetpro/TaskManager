namespace TaskManager.Api.Requests;

public sealed record ListNotificationsRequest(bool? UnreadOnly, int? Page, int? PageSize);
