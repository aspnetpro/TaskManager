using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using TaskManager.Api.Responses;
using TaskManager.Api.Security;

namespace TaskManager.Api.IntegrationTests.Fixtures;

/// <summary>
/// Helpers de auth com JWT real. Cookies são controlados na mão: o cookie de
/// refresh é Secure, e o CookieContainer não o reenviaria sobre http do TestServer.
/// </summary>
public static class AuthApi
{
    public const string BasePath = "/api/v1/auth";
    public const string Password = "correct-horse-battery-staple";

    public static HttpClient CreateAnonymousClient(this TaskManagerApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    public static object NewRegistration(string? email = null) => new
    {
        email = email ?? $"user-{Guid.NewGuid():N}@example.com",
        password = Password,
        fullName = "Ana Souza",
        workspaceName = "Acme"
    };

    public static async Task<Session> RegisterAsync(this HttpClient client, string? email = null)
    {
        var response = await client.PostAsJsonAsync($"{BasePath}/register", NewRegistration(email), TaskApi.Json);
        response.EnsureSuccessStatusCode();
        return await Session.FromAsync(response);
    }

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password, Guid? workspaceId = null) =>
        client.PostAsJsonAsync($"{BasePath}/login", new { email, password, workspaceId }, TaskApi.Json);

    public static Task<HttpResponseMessage> PostWithRefreshCookieAsync(this HttpClient client, string path, string? refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BasePath}/{path}");
        if (refreshToken is not null)
            request.Headers.Add(HeaderNames.Cookie, $"{RefreshTokenCookie.Name}={refreshToken}");
        return client.SendAsync(request);
    }

    public static SetCookieHeaderValue? RefreshCookie(this HttpResponseMessage response) =>
        response.Headers.TryGetValues(HeaderNames.SetCookie, out var values)
            ? SetCookieHeaderValue.ParseList(values.ToList()).FirstOrDefault(c => c.Name == RefreshTokenCookie.Name)
            : null;

    public static HttpClient WithBearer(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public sealed record Session(AuthResponse Body, string RefreshToken)
    {
        public static async Task<Session> FromAsync(HttpResponseMessage response)
        {
            var body = await response.ReadAsync<AuthResponse>();
            var cookie = response.RefreshCookie() ?? throw new InvalidOperationException("Refresh cookie missing.");
            return new Session(body, cookie.Value.ToString());
        }
    }
}
