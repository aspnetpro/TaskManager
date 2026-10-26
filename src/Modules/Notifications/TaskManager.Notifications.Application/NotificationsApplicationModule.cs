using Microsoft.Extensions.DependencyInjection;
using TaskManager.Notifications.Application.EventHandlers;

namespace TaskManager.Notifications.Application;

public static class NotificationsApplicationModule
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        var assembly = typeof(NotificationsApplicationModule).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Consumidores dos integration events do Tasks (entregues pelo Outbox).
        services.AddIntegrationEventHandler<TaskAssignedIntegrationEvent, TaskAssignedNotificationHandler>();
        services.AddIntegrationEventHandler<TaskAssignedIntegrationEvent, TaskAssignedEmailHandler>();
        services.AddIntegrationEventHandler<TaskStatusChangedIntegrationEvent, TaskStatusChangedNotificationHandler>();
        return services;
    }
}
