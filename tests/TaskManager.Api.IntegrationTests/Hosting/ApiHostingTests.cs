using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using TaskManager.Api.IntegrationTests.Fixtures;

namespace TaskManager.Api.IntegrationTests.Hosting;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ApiHostingTests(TaskManagerApiFactory factory)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/health")]
    public async Task HealthChecks_AreAnonymousAndHealthy(string path)
    {
        var response = await factory.CreateAnonymousClient().GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        var response = await factory.CreateAnonymousClient().GetAsync("/health/live");

        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("default-src 'none'");
    }

    [Fact]
    public async Task AuthEndpoints_OverLimit_Return429WithRetryAfter()
    {
        await using var limited = factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:AuthPermitLimit", "2"));
        var client = limited.CreateClient();
        var login = new { email = "nobody@example.com", password = "whatever-password" };

        await client.PostAsJsonAsync($"{AuthApi.BasePath}/login", login);
        await client.PostAsJsonAsync($"{AuthApi.BasePath}/login", login);
        var third = await client.PostAsJsonAsync($"{AuthApi.BasePath}/login", login);

        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        third.Headers.RetryAfter.Should().NotBeNull();
    }
}
