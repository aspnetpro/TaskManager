using Microsoft.Extensions.DependencyInjection;

namespace TaskManager.Auth.Application;

public static class AuthApplicationModule
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        var assembly = typeof(AuthApplicationModule).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddScoped<SessionIssuer>();
        return services;
    }
}
