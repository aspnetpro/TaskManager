using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Auth.Contracts;
using TaskManager.Auth.Infrastructure.Security;
using TaskManager.Shared.Application;
using TaskManager.Shared.Domain;

namespace TaskManager.Api.Security;

public static class SecurityExtensions
{
    public static IServiceCollection AddApiSecurity(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Mesmas JwtOptions que o módulo Auth usa para emitir: issuer, audience e chave não divergem.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>((bearer, jwt, timeProvider) =>
            {
                var settings = jwt.Value;
                bearer.TimeProvider = timeProvider;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = settings.CreateSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = WorkspaceClaimTypes.Name,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    // Validade conferida com o mesmo TimeProvider que emitiu o token.
                    LifetimeValidator = (notBefore, expires, _, parameters) =>
                    {
                        var now = timeProvider.GetUtcNow().UtcDateTime;
                        return (notBefore is null || notBefore <= now + parameters.ClockSkew)
                            && expires is not null && expires > now - parameters.ClockSkew;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.WorkspaceMember, p => p
                .RequireAuthenticatedUser()
                .AddRequirements(new WorkspaceRoleRequirement()))
            .AddPolicy(Policies.CanWriteTasks, p => p
                .RequireAuthenticatedUser()
                .AddRequirements(new WorkspaceRoleRequirement(
                    WorkspaceRole.Admin, WorkspaceRole.TechLead, WorkspaceRole.Developer)))
            .AddPolicy(Policies.AdminOnly, p => p
                .RequireAuthenticatedUser()
                .AddRequirements(new WorkspaceRoleRequirement(WorkspaceRole.Admin)))
            // Seguro por padrão: endpoint sem policy explícita exige membro de workspace.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new WorkspaceRoleRequirement())
                .Build());

        services.AddSingleton<IAuthorizationHandler, WorkspaceRoleHandler>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        return services;
    }
}
