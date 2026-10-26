using Microsoft.Extensions.DependencyInjection;

namespace TaskManager.Tasks.Application;

public static class TasksApplicationModule
{
    public static IServiceCollection AddTasksApplication(this IServiceCollection services)
    {
        var assembly = typeof(TasksApplicationModule).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddSingleton<TasksMetrics>();
        return services;
    }
}
