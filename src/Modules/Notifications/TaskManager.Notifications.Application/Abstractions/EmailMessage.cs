namespace TaskManager.Notifications.Application.Abstractions;

public sealed record EmailMessage(string To, string Subject, string Body);
