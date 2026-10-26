using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Shared.Application.Behaviors;

namespace TaskManager.Shared.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os pipeline behaviors uma única vez. Os módulos registram
    /// apenas seus handlers e validators (AddXxxModule).
    /// </summary>
    public static IServiceCollection AddSharedApplication(this IServiceCollection services)
    {
        // Ordem = ordem de execução: o span de tracing envolve validação e handler.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationBehavior<,>));
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
