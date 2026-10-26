namespace TaskManager.Notifications.Application.Abstractions;

public interface IEmailSender
{
    Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
