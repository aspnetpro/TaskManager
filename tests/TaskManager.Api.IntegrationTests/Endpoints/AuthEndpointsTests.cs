using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using TaskManager.Api.IntegrationTests.Fixtures;
using TaskManager.Shared.Domain;

namespace TaskManager.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuthEndpointsTests(TaskManagerApiFactory factory)
{
    // ---------- Registro ----------

    [Fact]
    public async Task Register_ValidRequest_Returns201WithAdminTokenAndHardenedCookie()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync($"{AuthApi.BasePath}/register", AuthApi.NewRegistration(), TaskApi.Json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await AuthApi.Session.FromAsync(response);
        session.Body.AccessToken.Should().NotBeNullOrWhiteSpace();
        session.Body.Workspace.Role.Should().Be(WorkspaceRole.Admin);
        session.Body.ExpiresAt.Should().Be(factory.Clock.GetUtcNow().AddMinutes(15));

        var cookie = response.RefreshCookie()!;
        cookie.HttpOnly.Should().BeTrue();
        cookie.Secure.Should().BeTrue();
        cookie.SameSite.Should().Be(SameSiteMode.Strict);
        cookie.Path.ToString().Should().Be(AuthApi.BasePath);
    }

    [Fact]
    public async Task Register_AccessToken_WorksOnTasksApi()
    {
        var client = factory.CreateAnonymousClient();
        var session = await client.RegisterAsync();

        var response = await client.WithBearer(session.Body.AccessToken)
            .PostTaskAsync(new { title = "First task", priority = "High" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.ReadAsync<Tasks.Application.Features.Tasks.TaskResponse>();
        task.ReporterId.Should().Be(session.Body.User.Id);
    }

    [Fact]
    public async Task Register_SameEmailDifferentCase_Returns409()
    {
        var client = factory.CreateAnonymousClient();
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        await client.RegisterAsync(email);

        var response = await client.PostAsJsonAsync(
            $"{AuthApi.BasePath}/register", AuthApi.NewRegistration(email.ToUpperInvariant()), TaskApi.Json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var response = await factory.CreateAnonymousClient().PostAsJsonAsync($"{AuthApi.BasePath}/register", new
        {
            email = "short@example.com",
            password = "123456",
            fullName = "Ana",
            workspaceName = "Acme"
        }, TaskApi.Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        problem.Errors.Keys.Should().Contain("Password");
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var client = factory.CreateAnonymousClient();
        var registered = await client.RegisterAsync();

        var response = await client.LoginAsync(registered.Body.User.Email, AuthApi.Password);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RefreshCookie().Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WrongPasswordOrUnknownEmail_ReturnSameError()
    {
        var client = factory.CreateAnonymousClient();
        var registered = await client.RegisterAsync();

        var wrongPassword = await client.LoginAsync(registered.Body.User.Email, "wrong-password-123");
        var unknownEmail = await client.LoginAsync($"nobody-{Guid.NewGuid():N}@example.com", AuthApi.Password);

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await wrongPassword.ReadAsync<ProblemDetails>()).Extensions["code"]!.ToString()
            .Should().Be((await unknownEmail.ReadAsync<ProblemDetails>()).Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task Login_WorkspaceUserIsNotMemberOf_Returns403()
    {
        var client = factory.CreateAnonymousClient();
        var registered = await client.RegisterAsync();
        var other = await client.RegisterAsync();

        var response = await client.LoginAsync(registered.Body.User.Email, AuthApi.Password, other.Body.Workspace.WorkspaceId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- Refresh ----------

    [Fact]
    public async Task Refresh_ValidCookie_RotatesRefreshToken()
    {
        var client = factory.CreateAnonymousClient();
        var session = await client.RegisterAsync();

        var response = await client.PostWithRefreshCookieAsync("refresh", session.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await AuthApi.Session.FromAsync(response);
        rotated.RefreshToken.Should().NotBe(session.RefreshToken);
        rotated.Body.User.Id.Should().Be(session.Body.User.Id);
    }

    [Fact]
    public async Task Refresh_ReusedToken_RevokesWholeFamily()
    {
        var client = factory.CreateAnonymousClient();
        var session = await client.RegisterAsync();
        var rotated = await AuthApi.Session.FromAsync(await client.PostWithRefreshCookieAsync("refresh", session.RefreshToken));

        // O token antigo volta: alguém tem uma cópia.
        var reuse = await client.PostWithRefreshCookieAsync("refresh", session.RefreshToken);
        var legit = await client.PostWithRefreshCookieAsync("refresh", rotated.RefreshToken);

        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        legit.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        reuse.Headers.GetValues(HeaderNames.SetCookie).Should().Contain(c => c.Contains("expires=Thu, 01 Jan 1970"));
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        var response = await factory.CreateAnonymousClient().PostWithRefreshCookieAsync("refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ThenRefresh_Returns401()
    {
        var client = factory.CreateAnonymousClient();
        var session = await client.RegisterAsync();

        var logout = await client.PostWithRefreshCookieAsync("logout", session.RefreshToken);
        var refresh = await client.PostWithRefreshCookieAsync("refresh", session.RefreshToken);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessToken_AfterExpiration_Returns401()
    {
        var client = factory.CreateAnonymousClient();
        var session = await client.RegisterAsync();

        // 15 min de vida + 30 s de clock skew.
        factory.Clock.Advance(TimeSpan.FromMinutes(16));
        var response = await client.WithBearer(session.Body.AccessToken).GetAsync(TaskApi.BasePath);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
