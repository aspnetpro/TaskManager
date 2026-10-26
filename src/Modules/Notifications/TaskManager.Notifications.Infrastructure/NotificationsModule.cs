using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskManager.Notifications.Application;
using TaskManager.Notifications.Application.Abstractions;
using TaskManager.Notifications.Application.EventHandlers;
using TaskManager.Notifications.Infrastructure.Email;
using TaskManager.Notifications.Infrastructure.Persistence;
using TaskManager.Shared.Infrastructure.Persistence;

namespace TaskManager.Notifications.Infrastructure;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNotificationsApplication();

        services.AddDbContext<NotificationsDbContext>((sp, options) => options
            .UseModuleDatabase(configuration, NotificationsDbContext.Schema)
            .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationsUnitOfWork, NotificationsUnitOfWork>();
        services.AddScoped<NotificationWriter>();

        AddEmail(services, configuration);
        return services;
    }

    /// <summary>Sem SendGrid:ApiKey configurada, o email só vai para o log (dev e testes).</summary>
    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SendGridOptions.SectionName);
        if (string.IsNullOrWhiteSpace(section[nameof(SendGridOptions.ApiKey)]))
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
            return;
        }

        services.AddOptions<SendGridOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Typed client: o factory reaproveita handlers (sem socket exhaustion) e o
        // pipeline de resiliência aplica retry com backoff, circuit breaker e timeouts.
        services.AddHttpClient<IEmailSender, SendGridEmailSender>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<SendGridOptions>>().Value;
                client.BaseAddress = options.BaseUrl;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            })
            .AddStandardResilienceHandler();
    }
}
