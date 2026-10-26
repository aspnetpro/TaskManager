using static TaskManager.Notifications.UnitTests.EventHandlers.NotificationHandlerFixture;

namespace TaskManager.Notifications.UnitTests.EventHandlers;

public sealed class TaskStatusChangedNotificationHandlerTests
{
    private readonly NotificationHandlerFixture _f = new();

    private TaskStatusChangedNotificationHandler Handler() => new(_f.Writer(), _f.Clock);

    [Fact]
    public async Task HandleAsync_ChangedByThirdParty_NotifiesReporterAndAssignee()
    {
        await Handler().HandleAsync(StatusChanged(Lead, Assignee), CancellationToken.None);

        _f.Written().Select(n => n.RecipientId).Should().BeEquivalentTo([Reporter, Assignee]);
        _f.Written().Should().OnlyContain(n => n.Detail == "InProgress" && n.Type == NotificationType.TaskStatusChanged);
    }

    [Fact]
    public async Task HandleAsync_ChangedByAssignee_NotifiesOnlyReporter()
    {
        await Handler().HandleAsync(StatusChanged(Assignee, Assignee), CancellationToken.None);

        _f.Written().Should().ContainSingle().Which.RecipientId.Should().Be(Reporter);
    }

    [Fact]
    public async Task HandleAsync_ReporterIsAssigneeAndChangedIt_NotifiesNobody()
    {
        await Handler().HandleAsync(StatusChanged(Reporter, Reporter), CancellationToken.None);

        _f.Written().Should().BeEmpty();
        await _f.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
