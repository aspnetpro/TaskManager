using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Api.IntegrationTests.Fixtures;

namespace TaskManager.Api.IntegrationTests.Hosting;

/// <summary>
/// O rate limit por IP só funciona se o IP for o do cliente, não o do proxy.
/// Com o IP de conexão controlado pelo teste, simulamos o nginx (rede privada) e
/// um cliente conectando direto (IP público) tentando forjar X-Forwarded-For.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ForwardedHeadersTests(TaskManagerApiFactory factory)
{
    private static readonly IPAddress Proxy = IPAddress.Parse("172.18.0.5");
    private static readonly IPAddress PublicClient = IPAddress.Parse("198.51.100.7");

    [Fact]
    public async Task AuthRateLimit_BehindTrustedProxy_PartitionsByForwardedClientIp()
    {
        await using var api = WithAuthLimit(2);

        await LoginAsync(api, Proxy, "203.0.113.10");
        await LoginAsync(api, Proxy, "203.0.113.10");
        var third = await LoginAsync(api, Proxy, "203.0.113.10");
        var otherClient = await LoginAsync(api, Proxy, "203.0.113.11");

        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        otherClient.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthRateLimit_SpoofedForwardedFor_FromUntrustedSource_IsIgnored()
    {
        await using var api = WithAuthLimit(2);

        // Cada request "jura" ser de um IP diferente; todas vêm do mesmo IP público.
        await LoginAsync(api, PublicClient, "203.0.113.20");
        await LoginAsync(api, PublicClient, "203.0.113.21");
        var third = await LoginAsync(api, PublicClient, "203.0.113.22");

        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task AuthRateLimit_ClientSpoofsHeaderThroughProxy_UsesRealClientIp()
    {
        await using var api = WithAuthLimit(2);

        // Cliente manda XFF forjado; o nginx acrescenta o IP real à direita.
        await LoginAsync(api, Proxy, "10.9.9.1, 203.0.113.30");
        await LoginAsync(api, Proxy, "10.9.9.2, 203.0.113.30");
        var third = await LoginAsync(api, Proxy, "10.9.9.3, 203.0.113.30");

        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private WebApplicationFactory<Program> WithAuthLimit(int limit) =>
        factory.WithWebHostBuilder(b => b
            .UseSetting("RateLimiting:AuthPermitLimit", limit.ToString(CultureInfo.InvariantCulture))
            .ConfigureTestServices(services => services.AddSingleton<IStartupFilter, RemoteIpFromHeader>()));

    private static async Task<HttpResponseMessage> LoginAsync(WebApplicationFactory<Program> api, IPAddress remoteIp, string forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{AuthApi.BasePath}/login")
        {
            Content = JsonContent.Create(new { email = "nobody@example.com", password = "whatever-password" })
        };
        request.Headers.Add(RemoteIpFromHeader.HeaderName, remoteIp.ToString());
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return await api.CreateClient().SendAsync(request);
    }

    /// <summary>
    /// TestServer não tem IP de conexão. Este filtro roda antes do pipeline da Api
    /// (antes do UseForwardedHeaders) e faz o papel da conexão TCP.
    /// </summary>
    private sealed class RemoteIpFromHeader : IStartupFilter
    {
        public const string HeaderName = "X-Test-Remote-Ip";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (IPAddress.TryParse(context.Request.Headers[HeaderName], out var ip))
                    context.Connection.RemoteIpAddress = ip;
                await nextMiddleware(context);
            });
            next(app);
        };
    }
}
