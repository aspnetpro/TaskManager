using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Shared.Application.Events;
using TaskManager.Shared.Infrastructure.Outbox;
using TaskManager.Shared.Infrastructure.Persistence;
using TaskManager.Tasks.Application;
using TaskManager.Tasks.Contracts.IntegrationEvents;
using TaskManager.Tasks.Infrastructure.Persistence;

namespace TaskManager.Tasks.Infrastructure;

public static class TasksModule
{
    public static IServiceCollection AddTasksModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTasksApplication();

        // Contratos publicados pelo Tasks: o registry precisa conhecê-los mesmo sem consumidor.
        services.AddIntegrationEvent<TaskAssignedIntegrationEvent>();
        services.AddIntegrationEvent<TaskStatusChangedIntegrationEvent>();
        services.AddSingleton<TasksOutboxInterceptor>();
        services.AddDbContext<TasksDbContext>((sp, options) => options
            .UseModuleDatabase(configuration, TasksDbContext.Schema)
            .AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<TasksOutboxInterceptor>()));
        services.AddOutboxProcessor<TasksDbContext>();

        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITasksUnitOfWork, TasksUnitOfWork>();
        return services;
    }
}
