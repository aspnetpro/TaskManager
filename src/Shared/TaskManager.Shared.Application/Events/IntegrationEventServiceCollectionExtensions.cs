using Microsoft.Extensions.DependencyInjection;

namespace TaskManager.Shared.Application.Events;

public static class IntegrationEventServiceCollectionExtensions
{
    /// <summary>Registra o handler e o tipo do evento no registry do Outbox.</summary>
    public static IServiceCollection AddIntegrationEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : class
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
        return services.AddIntegrationEvent<TEvent>();
    }

    /// <summary>
    /// Registra um integration event que o módulo publica, tenha consumidor ou não.
    /// O processor só lê do Outbox os tipos que o registry conhece: evento de uma
    /// versão mais nova (no meio de um deploy) fica para a réplica que o conhece.
    /// </summary>
    public static IServiceCollection AddIntegrationEvent<TEvent>(this IServiceCollection services)
        where TEvent : class
    {
        RegistryOf(services).Register(typeof(TEvent));
        return services;
    }

    private static IntegrationEventRegistry RegistryOf(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IntegrationEventRegistry));
        if (descriptor?.ImplementationInstance is IntegrationEventRegistry existing)
            return existing;

        var registry = new IntegrationEventRegistry();
        services.AddSingleton(registry);
        return registry;
    }
}
