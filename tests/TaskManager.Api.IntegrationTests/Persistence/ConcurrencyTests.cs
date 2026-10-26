using Microsoft.EntityFrameworkCore;
using TaskManager.Shared.Application;
using TaskManager.Shared.Domain;
using TaskManager.Shared.Infrastructure.Persistence;
using TaskManager.Tasks.Domain.Entities;
using TaskManager.Tasks.Domain.Enums;
using TaskManager.Tasks.Domain.ValueObjects;
using TaskManager.Tasks.Infrastructure.Persistence;
using TaskManager.Api.IntegrationTests.Fixtures;

namespace TaskManager.Api.IntegrationTests.Persistence;

/// <summary>
/// A corrida que o check de versão no handler não cobre: dois requests leem
/// a mesma versão e salvam em sequência. O concurrency token no SQL barra o segundo.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ConcurrencyTests(TaskManagerApiFactory factory)
{
    [Fact]
    public async Task SaveChanges_TwoWritersSameVersion_SecondThrowsConcurrencyException()
    {
        var workspaceId = Guid.CreateVersion7();
        var taskId = await SeedTaskAsync(workspaceId);

        await using var first = CreateContext(workspaceId);
        await using var second = CreateContext(workspaceId);
        var a = await first.Tasks.SingleAsync(t => t.Id == taskId);
        var b = await second.Tasks.SingleAsync(t => t.Id == taskId);

        a.Rename(Title.Create("First writer").Value);
        await first.SaveChangesAsync();

        b.Rename(Title.Create("Second writer").Value);
        var act = () => second.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        a.Version.Should().Be(1);
    }

    private async Task<Guid> SeedTaskAsync(Guid workspaceId)
    {
        await using var context = CreateContext(workspaceId);
        var task = TaskItem.Create(workspaceId, Guid.NewGuid(), Title.Create("Race").Value, null, TaskPriority.Low, null);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        return task.Id;
    }

    private TasksDbContext CreateContext(Guid workspaceId)
    {
        var options = new DbContextOptionsBuilder<TasksDbContext>()
            .UseSqlServer(factory.ConnectionString)
            .AddInterceptors(new AuditableEntityInterceptor(factory.Clock))
            .Options;

        return new TasksDbContext(options, new FixedUser(workspaceId));
    }

    private sealed class FixedUser(Guid workspaceId) : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid WorkspaceId => workspaceId;
        public WorkspaceRole Role => WorkspaceRole.Admin;
    }
}
