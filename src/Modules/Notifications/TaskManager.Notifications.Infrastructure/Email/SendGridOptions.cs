using System.ComponentModel.DataAnnotations;

namespace TaskManager.Notifications.Infrastructure.Email;

public sealed class SendGridOptions
{
    public const string SectionName = "SendGrid";

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string FromEmail { get; set; } = string.Empty;

    public Uri BaseUrl { get; set; } = new("https://api.sendgrid.com");
}
