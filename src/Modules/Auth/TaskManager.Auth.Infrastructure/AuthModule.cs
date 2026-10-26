using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Auth.Application;
using TaskManager.Auth.Application.Abstractions;
using TaskManager.Auth.Contracts;
using TaskManager.Auth.Infrastructure.Persistence;
using TaskManager.Auth.Infrastructure.Security;
using TaskManager.Auth.Infrastructure.Services;
using TaskManager.Shared.Infrastructure.Persistence;

namespace TaskManager.Auth.Infrastructure;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthApplication();

        services.AddDbContext<AuthDbContext>((sp, options) => options
            .UseModuleDatabase(configuration, AuthDbContext.Schema)
            .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuthUnitOfWork, AuthUnitOfWork>();

        // Falha no startup se faltar issuer, audience ou chave — não no primeiro login.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        services.AddScoped<IWorkspaceMembershipService, WorkspaceMembershipService>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        return services;
    }
}
