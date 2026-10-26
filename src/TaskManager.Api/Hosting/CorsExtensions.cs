namespace TaskManager.Api.Hosting;

public static class CorsExtensions
{
    public const string SectionName = "Cors:AllowedOrigins";

    /// <summary>
    /// Origens explícitas por ambiente. AllowCredentials é obrigatório para o
    /// cookie de refresh — e é exatamente por isso que "*" não é permitido.
    /// </summary>
    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection(SectionName).Get<string[]>() ?? [];

        services.AddCors(cors => cors.AddDefaultPolicy(policy =>
        {
            if (origins.Length == 0)
                return;

            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .WithExposedHeaders("ETag", "Location", "Retry-After")
                .AllowCredentials();
        }));

        return services;
    }
}
