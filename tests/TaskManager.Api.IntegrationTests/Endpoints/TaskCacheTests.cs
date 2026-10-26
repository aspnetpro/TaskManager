using TaskManager.Api.IntegrationTests.Fixtures;
using TaskManager.Shared.Domain;
using TaskManager.Tasks.Application.Features.Tasks;

namespace TaskManager.Api.IntegrationTests.Endpoints;

/// <summary>GET /tasks/{id} é cacheado; os commands precisam invalidar.</summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class TaskCacheTests(TaskManagerApiFactory factory)
{
    [Fact]
    public async Task Get_AfterTransition_ReturnsFreshData()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        var task = await client.CreateTaskAsync(new { title = "Cache me", priority = "Low" });
        await client.GetAsync($"{TaskApi.BasePath}/{task.Id}"); // popula o cache

        await client.PatchJsonAsync($"{TaskApi.BasePath}/{task.Id}/status", new { status = "InProgress" });
        var details = await (await client.GetAsync($"{TaskApi.BasePath}/{task.Id}")).ReadAsync<TaskDetailsResponse>();

        details.Status.Should().Be(Tasks.Domain.Enums.TaskItemStatus.InProgress);
        details.Version.Should().Be(1);
    }

    [Fact]
    public async Task Get_SameTaskIdFromAnotherWorkspace_DoesNotHitCache()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var other = await factory.CreateWorkspaceAsync();
        var task = await ws.ClientAs(WorkspaceRole.Developer).CreateTaskAsync(new { title = "Secret", priority = "Low" });
        await ws.ClientAs(WorkspaceRole.Developer).GetAsync($"{TaskApi.BasePath}/{task.Id}"); // cacheado no ws

        var response = await other.ClientAs(WorkspaceRole.Admin).GetAsync($"{TaskApi.BasePath}/{task.Id}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
