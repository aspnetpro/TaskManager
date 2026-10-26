using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Api.IntegrationTests.Fixtures;
using TaskManager.Shared.Domain;
using TaskManager.Shared.Infrastructure.Outbox;
using TaskManager.Tasks.Infrastructure.Persistence;

namespace TaskManager.Api.IntegrationTests.Persistence;

/// <summary>
/// Rolling deploy: a revisão nova grava um integration event que a antiga não conhece.
/// A antiga não pode dar a mensagem como processada — o evento sumiria sem alerta.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class OutboxProcessorTests(TaskManagerApiFactory factory)
{
    [Fact]
    public async Task Process_UnknownEventType_StaysPendingWithoutAttempts()
    {
        var unknown = new OutboxMessage("TaskManager.Future.SomethingHappenedIntegrationEvent", "{}", factory.Clock.GetUtcNow());
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TasksDbContext>();
            context.Set<OutboxMessage>().Add(unknown);
            await context.SaveChangesAsync();
        }

        // Um evento conhecido gravado depois prova que o processor passou por cima do desconhecido.
        await AssignTaskAndWaitForProcessingAsync();

        await using var check = factory.Services.CreateAsyncScope();
        var stored = await check.ServiceProvider.GetRequiredService<TasksDbContext>()
            .Set<OutboxMessage>().AsNoTracking().SingleAsync(m => m.Id == unknown.Id);
        stored.ProcessedAt.Should().BeNull();
        stored.Attempts.Should().Be(0);
    }

    private async Task AssignTaskAndWaitForProcessingAsync()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var lead = ws.ClientAs(WorkspaceRole.TechLead);
        var task = await lead.CreateTaskAsync(new { title = "Known event", priority = "Low" });
        await lead.PutAsJsonAsync($"{TaskApi.BasePath}/{task.Id}/assignee",
            new { assigneeId = ws.UserId(WorkspaceRole.Developer) }, TaskApi.Json);

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var processedAt = await scope.ServiceProvider.GetRequiredService<TasksDbContext>()
                .Set<OutboxMessage>().AsNoTracking()
                .Where(m => m.Type.EndsWith("TaskAssignedIntegrationEvent") && m.Content.Contains(task.Id.ToString()))
                .Select(m => m.ProcessedAt)
                .ToListAsync();
            if (processedAt is [not null])
                return;

            await Task.Delay(100);
        }

        throw new TimeoutException("The known integration event was never processed.");
    }
}
