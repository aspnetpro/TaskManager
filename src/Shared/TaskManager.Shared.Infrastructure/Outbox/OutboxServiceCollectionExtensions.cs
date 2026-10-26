using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TaskManager.Shared.Infrastructure.Outbox;

public static class OutboxServiceCollectionExtensions
{
    /// <summary>Liga o processor de Outbox para o DbContext de um módulo.</summary>
    public static IServiceCollection AddOutboxProcessor<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddHostedService<OutboxProcessor<TContext>>();
        return services;
    }
}
