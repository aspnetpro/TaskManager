using Microsoft.Extensions.Logging;
using TaskManager.Notifications.Application.Abstractions;
using TaskManager.Shared.Domain.Results;

namespace TaskManager.Notifications.Infrastructure.Email;

/// <summary>Fake para dev e testes: registra o email no log em vez de enviar.</summary>
internal sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        LogEmail(logger, message.To, message.Subject);
        return Task.FromResult(Result.Success());
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email (not sent, no provider configured) to {To}: {Subject}")]
    private static partial void LogEmail(ILogger logger, string to, string subject);
}
