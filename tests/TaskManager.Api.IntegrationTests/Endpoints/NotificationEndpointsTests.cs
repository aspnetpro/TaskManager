using System.Net;
using System.Net.Http.Json;
using TaskManager.Api.IntegrationTests.Fixtures;
using TaskManager.Notifications.Application.Features.Inbox;
using TaskManager.Notifications.Domain;
using TaskManager.Shared.Application;
using TaskManager.Shared.Domain;

namespace TaskManager.Api.IntegrationTests.Endpoints;

/// <summary>
/// Fluxo ponta a ponta do Outbox: o command salva a tarefa e o evento na mesma
/// transação, o processor despacha, o módulo Notifications grava. Eventual: os
/// testes esperam a notificação aparecer.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class NotificationEndpointsTests(TaskManagerApiFactory factory)
{
    private const string BasePath = "/api/v1/notifications";

    [Fact]
    public async Task AssignTask_NotifiesAssigneeOnly()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var lead = ws.ClientAs(WorkspaceRole.TechLead);
        var developer = ws.ClientAs(WorkspaceRole.Developer);
        var task = await lead.CreateTaskAsync(new { title = "Review PR", priority = "High" });

        await lead.PutAsJsonAsync($"{TaskApi.BasePath}/{task.Id}/assignee",
            new { assigneeId = ws.UserId(WorkspaceRole.Developer) }, TaskApi.Json);

        var inbox = await WaitForNotificationsAsync(developer, expected: 1);
        inbox.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Type = NotificationType.TaskAssigned,
            TaskId = task.Id,
            TaskTitle = "Review PR",
            ActorId = ws.UserId(WorkspaceRole.TechLead)
        });
        (await ListAsync(lead)).TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task StartTask_NotifiesReporterWithNewStatus()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var admin = ws.ClientAs(WorkspaceRole.Admin);
        var developer = ws.ClientAs(WorkspaceRole.Developer);
        var task = await admin.CreateTaskAsync(new { title = "Fix login", priority = "Medium" });

        // Developer inicia sem responsável: vira assignee (auto-atribuição não notifica ninguém).
        await developer.PatchJsonAsync($"{TaskApi.BasePath}/{task.Id}/status", new { status = "InProgress" });

        var inbox = await WaitForNotificationsAsync(admin, expected: 1);
        inbox.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Type = NotificationType.TaskStatusChanged,
            Detail = "InProgress",
            ActorId = ws.UserId(WorkspaceRole.Developer)
        });
        (await ListAsync(developer)).TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task MarkRead_OwnNotification_RemovesFromUnread()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var developer = ws.ClientAs(WorkspaceRole.Developer);
        var notification = await AssignToDeveloperAndWaitAsync(ws);

        var response = await developer.PostAsync($"{BasePath}/{notification.Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListAsync(developer, "?unreadOnly=true")).TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task MarkRead_SomeoneElsesNotification_Returns404()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var notification = await AssignToDeveloperAndWaitAsync(ws);

        var response = await ws.ClientAs(WorkspaceRole.Admin).PostAsync($"{BasePath}/{notification.Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<NotificationResponse> AssignToDeveloperAndWaitAsync(TestWorkspace ws)
    {
        var lead = ws.ClientAs(WorkspaceRole.TechLead);
        var task = await lead.CreateTaskAsync(new { title = "Deploy", priority = "Low" });
        await lead.PutAsJsonAsync($"{TaskApi.BasePath}/{task.Id}/assignee",
            new { assigneeId = ws.UserId(WorkspaceRole.Developer) }, TaskApi.Json);

        var inbox = await WaitForNotificationsAsync(ws.ClientAs(WorkspaceRole.Developer), expected: 1);
        return inbox.Items[0];
    }

    private static Task<PagedResponse<NotificationResponse>> ListAsync(HttpClient client, string query = "") =>
        client.GetFromJsonAsync<PagedResponse<NotificationResponse>>($"{BasePath}{query}", TaskApi.Json)!;

    private static async Task<PagedResponse<NotificationResponse>> WaitForNotificationsAsync(HttpClient client, int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var page = await ListAsync(client);
            if (page.TotalCount >= expected || DateTime.UtcNow > deadline)
                return page;

            await Task.Delay(100);
        }
    }
}
