using Microsoft.AspNetCore.HttpOverrides;

namespace TaskManager.Api.Hosting;

public static class ForwardedHeadersExtensions
{
    public const string SectionName = "ForwardedHeaders:KnownNetworks";

    /// <summary>
    /// Atrás de proxy (nginx no compose; nginx + ingress do Container Apps no Azure),
    /// o RemoteIpAddress é o do proxy — e o rate limit por IP vira um balde só para
    /// todo mundo. Aqui o X-Forwarded-For é lido da direita para a esquerda, pulando
    /// só os saltos vindos de redes confiáveis: o primeiro IP fora delas é o cliente.
    /// Um X-Forwarded-For forjado pelo cliente fica à esquerda dele e é ignorado.
    /// NÃO use ASPNETCORE_FORWARDEDHEADERS_ENABLED: ele confia em qualquer origem e
    /// só lê o último salto (ForwardLimit = 1), o que erra com dois proxies.
    /// </summary>
    public static IServiceCollection AddApiForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var networks = configuration.GetSection(SectionName).Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = null;

            // Loopback continua confiável (default); o resto vem da configuração.
            foreach (var network in networks)
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        });

        return services;
    }
}
