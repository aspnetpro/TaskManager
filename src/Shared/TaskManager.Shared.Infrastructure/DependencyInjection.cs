using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Shared.Application;
using TaskManager.Shared.Application.Caching;
using TaskManager.Shared.Application.Events;
using TaskManager.Shared.Infrastructure.Caching;
using TaskManager.Shared.Infrastructure.Outbox;
using TaskManager.Shared.Infrastructure.Persistence;

namespace TaskManager.Shared.Infrastructure;

public static class DependencyInjection
{
    public const string RedisConnectionStringName = "Redis";

    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntityInterceptor>();

        // Outbox
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IntegrationEventDispatcher>();
        services.AddSingleton<OutboxDiagnostics>();
        if (services.All(d => d.ServiceType != typeof(IntegrationEventRegistry)))
            services.AddSingleton(new IntegrationEventRegistry());

        // Cache: sem Redis configurado, o HybridCache roda só com o L1 em memória;
        // com Redis, só com o L2 (ver HybridWorkspaceCache).
        var caching = configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>() ?? new CachingOptions();
        var redis = configuration.GetConnectionString(RedisConnectionStringName);
        var hasRedis = !string.IsNullOrWhiteSpace(redis);
        if (hasRedis)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redis;
                options.InstanceName = "taskmanager:";
            });
        }

        services.AddHybridCache(options => options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration = caching.Expiration,
            LocalCacheExpiration = caching.LocalExpiration
        });
        services.AddScoped<IWorkspaceCache>(sp => new HybridWorkspaceCache(
            sp.GetRequiredService<HybridCache>(),
            sp.GetRequiredService<ICurrentUser>(),
            useLocalCache: !hasRedis));

        return services;
    }
}
