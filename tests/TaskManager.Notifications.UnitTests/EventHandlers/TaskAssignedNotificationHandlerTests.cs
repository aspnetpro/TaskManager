using static TaskManager.Notifications.UnitTests.EventHandlers.NotificationHandlerFixture;

namespace TaskManager.Notifications.UnitTests.EventHandlers;

public sealed class TaskAssignedNotificationHandlerTests
{
    private readonly NotificationHandlerFixture _f = new();

    private TaskAssignedNotificationHandler Handler() => new(_f.Writer(), _f.Clock);

    [Fact]
    public async Task HandleAsync_AssignedBySomeoneElse_NotifiesAssignee()
    {
        var e = Assigned(Assignee, Lead);

        await Handler().HandleAsync(e, CancellationToken.None);

        _f.Written().Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            WorkspaceId,
            RecipientId = Assignee,
            Type = NotificationType.TaskAssigned,
            e.TaskId,
            TaskTitle = "Review PR",
            ActorId = Lead,
            SourceEventId = e.EventId,
            CreatedAt = _f.Clock.GetUtcNow()
        });
    }

    [Fact]
    public async Task HandleAsync_SelfAssignment_NotifiesNobody()
    {
        await Handler().HandleAsync(Assigned(Assignee, Assignee), CancellationToken.None);

        _f.Written().Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Unassigned_NotifiesNobody()
    {
        await Handler().HandleAsync(Assigned(null, Lead), CancellationToken.None);

        _f.Written().Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_EventAlreadyProcessed_IsIdempotent()
    {
        _f.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.DuplicateKey));

        var act = () => Handler().HandleAsync(Assigned(Assignee, Lead), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_SaveFails_ThrowsSoOutboxRetries()
    {
        _f.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var act = () => Handler().HandleAsync(Assigned(Assignee, Lead), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
