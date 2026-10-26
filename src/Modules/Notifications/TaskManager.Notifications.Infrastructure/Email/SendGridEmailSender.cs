using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManager.Notifications.Application.Abstractions;
using TaskManager.Shared.Domain.Results;

namespace TaskManager.Notifications.Infrastructure.Email;

/// <summary>Typed HttpClient para a API v3 do SendGrid. Nunca instancie HttpClient na mão.</summary>
internal sealed partial class SendGridEmailSender(
    HttpClient httpClient,
    IOptions<SendGridOptions> options,
    ILogger<SendGridEmailSender> logger)
    : IEmailSender
{
    private static readonly Error DeliveryFailed = Error.Unexpected("Email.DeliveryFailed", "Email provider rejected the message.");

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            personalizations = new[] { new { to = new[] { new { email = message.To } } } },
            from = new { email = options.Value.FromEmail },
            subject = message.Subject,
            content = new[] { new { type = "text/plain", value = message.Body } }
        };

        using var response = await httpClient.PostAsJsonAsync("/v3/mail/send", payload, cancellationToken);
        if (response.IsSuccessStatusCode)
            return Result.Success();

        LogDeliveryFailed(logger, (int)response.StatusCode);
        return DeliveryFailed;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "SendGrid returned {StatusCode}")]
    private static partial void LogDeliveryFailed(ILogger logger, int statusCode);
}
