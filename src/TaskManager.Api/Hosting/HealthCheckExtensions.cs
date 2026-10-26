using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskManager.Auth.Infrastructure.Persistence;
using TaskManager.Shared.Infrastructure;

namespace TaskManager.Api.Hosting;

public static class HealthCheckExtensions
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // Um banco para todos os módulos (schemas separados): um check basta.
        var checks = services.AddHealthChecks()
            .AddDbContextCheck<AuthDbContext>("database", tags: [ReadyTag]);

        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString(DependencyInjection.RedisConnectionStringName)))
            checks.AddCheck<RedisHealthCheck>("redis", tags: [ReadyTag]);

        return services;
    }

    /// <summary>
    /// /health/live: o processo responde (liveness — reiniciar se falhar).
    /// /health/ready: dependências ok (readiness — tirar do balanceador se falhar).
    /// </summary>
    public static IEndpointRouteBuilder MapApiHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous()
            .DisableRateLimiting();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains(ReadyTag) })
            .AllowAnonymous()
            .DisableRateLimiting();

        app.MapHealthChecks("/health")
            .AllowAnonymous()
            .DisableRateLimiting();

        return app;
    }
}
