using System.Net;
using System.Net.Http.Json;
using TaskManager.Api.IntegrationTests.Fixtures;
using TaskManager.Api.Responses;
using TaskManager.Auth.Application.Features.Members;
using TaskManager.Shared.Domain;

namespace TaskManager.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MemberEndpointsTests(TaskManagerApiFactory factory)
{
    private const string BasePath = "/api/v1/workspace/members";

    [Fact]
    public async Task List_ReturnsMembersOfTokenWorkspace()
    {
        var client = factory.CreateAnonymousClient();
        var session = await client.RegisterAsync();

        var members = await client.WithBearer(session.Body.AccessToken)
            .GetFromJsonAsync<List<MemberResponse>>(BasePath, TaskApi.Json);

        members.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            UserId = session.Body.User.Id,
            Role = WorkspaceRole.Admin,
            IsActive = true
        });
    }

    [Fact]
    public async Task Add_AsAdmin_NewMemberCanLoginIntoWorkspace()
    {
        var anonymous = factory.CreateAnonymousClient();
        var admin = await anonymous.RegisterAsync();
        var invitee = await factory.CreateAnonymousClient().RegisterAsync();

        var added = await factory.CreateAnonymousClient().WithBearer(admin.Body.AccessToken)
            .PostAsJsonAsync(BasePath, new { email = invitee.Body.User.Email, role = "Developer" }, TaskApi.Json);
        var login = await anonymous.LoginAsync(invitee.Body.User.Email, AuthApi.Password, admin.Body.Workspace.WorkspaceId);

        added.StatusCode.Should().Be(HttpStatusCode.Created);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await login.ReadAsync<AuthResponse>();
        session.Workspace.Role.Should().Be(WorkspaceRole.Developer);
    }

    [Fact]
    public async Task Add_ExistingMember_Returns409()
    {
        var client = factory.CreateAnonymousClient();
        var admin = await client.RegisterAsync();

        var response = await client.WithBearer(admin.Body.AccessToken)
            .PostAsJsonAsync(BasePath, new { email = admin.Body.User.Email, role = "Viewer" }, TaskApi.Json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Add_UnknownEmail_Returns404()
    {
        var client = factory.CreateAnonymousClient();
        var admin = await client.RegisterAsync();

        var response = await client.WithBearer(admin.Body.AccessToken)
            .PostAsJsonAsync(BasePath, new { email = "ghost@example.com", role = "Viewer" }, TaskApi.Json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(WorkspaceRole.TechLead)]
    [InlineData(WorkspaceRole.Developer)]
    public async Task Add_AsNonAdmin_Returns403(WorkspaceRole role)
    {
        var ws = await factory.CreateWorkspaceAsync();

        var response = await ws.ClientAs(role)
            .PostAsJsonAsync(BasePath, new { email = "x@example.com", role = "Viewer" }, TaskApi.Json);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
