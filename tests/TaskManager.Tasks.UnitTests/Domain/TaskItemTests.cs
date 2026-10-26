namespace TaskManager.Tasks.UnitTests.Domain;

public sealed class TaskItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    [Fact]
    public void Create_ValidData_StartsPendingAndRaisesCreatedEvent()
    {
        var task = TaskItem.Create(
            TestUsers.WorkspaceId, TestUsers.ReporterId,
            Title.Create("Implement auth").Value, null, TaskPriority.High, null);

        task.Status.Should().Be(TaskItemStatus.Pending);
        task.ReporterId.Should().Be(TestUsers.ReporterId);
        task.AssigneeId.Should().BeNull();
        task.DomainEvents.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new TaskCreatedEvent(task.Id, TestUsers.WorkspaceId, TestUsers.ReporterId));
    }

    [Theory]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.Cancelled)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Cancelled)]
    public void TransitionTo_ValidTransition_ChangesStatusAndRecordsHistory(TaskItemStatus from, TaskItemStatus to)
    {
        var task = new TaskItemBuilder().WithAssignee(TestUsers.ReporterId).InStatus(from).Build();
        var historyBefore = task.StatusHistory.Count;

        var result = task.TransitionTo(to, TestUsers.ReporterId, Now);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(to);
        task.StatusHistory.Should().HaveCount(historyBefore + 1);
        task.StatusHistory[^1].Should().BeEquivalentTo(new { From = from, To = to, ChangedBy = TestUsers.ReporterId, ChangedAt = Now });
        task.DomainEvents.OfType<TaskStatusChangedEvent>().Should().ContainSingle(e => e.From == from && e.To == to);
    }

    [Theory]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.Pending)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Pending)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.Pending)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.Cancelled)]
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.Pending)]
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.Done)]
    public void TransitionTo_InvalidTransition_ReturnsBusinessRuleErrorAndKeepsStatus(TaskItemStatus from, TaskItemStatus to)
    {
        var task = new TaskItemBuilder().InStatus(from).Build();

        var result = task.TransitionTo(to, TestUsers.ReporterId, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Task.InvalidTransition");
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
        task.Status.Should().Be(from);
        task.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Start_WithoutAssignee_AssignsToUserWhoStarted()
    {
        var task = new TaskItemBuilder().Build();

        task.Start(TestUsers.OtherUserId, Now);

        task.AssigneeId.Should().Be(TestUsers.OtherUserId);
        task.DomainEvents.OfType<TaskAssignedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Start_WithAssignee_KeepsExistingAssignee()
    {
        var task = new TaskItemBuilder().WithAssignee(TestUsers.ReporterId).Build();

        task.Start(TestUsers.OtherUserId, Now);

        task.AssigneeId.Should().Be(TestUsers.ReporterId);
    }

    [Fact]
    public void Complete_InProgressTask_SetsCompletedAt()
    {
        var task = new TaskItemBuilder().InStatus(TaskItemStatus.InProgress).Build();

        task.Complete(TestUsers.ReporterId, Now);

        task.CompletedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData(TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.Cancelled)]
    public void AssignTo_ClosedTask_ReturnsError(TaskItemStatus status)
    {
        var task = new TaskItemBuilder().InStatus(status).Build();

        var result = task.AssignTo(TestUsers.OtherUserId, TestUsers.ReporterId);

        result.Error.Code.Should().Be("Task.ClosedTaskAssignment");
    }

    [Fact]
    public void AssignTo_NewAssignee_RaisesEventWithWhoAssigned()
    {
        var task = new TaskItemBuilder().Build();

        task.AssignTo(TestUsers.OtherUserId, TestUsers.ReporterId);

        task.DomainEvents.OfType<TaskAssignedEvent>().Should().ContainSingle()
            .Which.Should().Be(new TaskAssignedEvent(task.Id, null, TestUsers.OtherUserId, TestUsers.ReporterId));
    }

    [Fact]
    public void Delete_ActiveTask_MarksDeleted()
    {
        var task = new TaskItemBuilder().Build();

        var result = task.Delete(TestUsers.ReporterId, Now);

        result.IsSuccess.Should().BeTrue();
        task.IsDeleted.Should().BeTrue();
        task.DeletedAt.Should().Be(Now);
        task.DeletedBy.Should().Be(TestUsers.ReporterId);
    }

    [Fact]
    public void Delete_AlreadyDeleted_ReturnsError()
    {
        var task = new TaskItemBuilder().Build();
        task.Delete(TestUsers.ReporterId, Now);

        task.Delete(TestUsers.ReporterId, Now).Error.Code.Should().Be("Task.AlreadyDeleted");
    }

    [Fact]
    public void Restore_WithinWindow_Undeletes()
    {
        var task = new TaskItemBuilder().Build();
        task.Delete(TestUsers.ReporterId, Now);

        var result = task.Restore(Now.AddDays(TaskItem.RestoreWindowDays));

        result.IsSuccess.Should().BeTrue();
        task.IsDeleted.Should().BeFalse();
        task.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Restore_AfterWindow_ReturnsError()
    {
        var task = new TaskItemBuilder().Build();
        task.Delete(TestUsers.ReporterId, Now);

        var result = task.Restore(Now.AddDays(TaskItem.RestoreWindowDays).AddSeconds(1));

        result.Error.Code.Should().Be("Task.RestoreWindowExpired");
        task.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Restore_NotDeleted_ReturnsError()
    {
        new TaskItemBuilder().Build().Restore(Now).Error.Code.Should().Be("Task.NotDeleted");
    }

    [Theory]
    [InlineData(-1, TaskItemStatus.Pending, true)]
    [InlineData(-1, TaskItemStatus.InProgress, true)]
    [InlineData(-1, TaskItemStatus.Done, false)]
    [InlineData(-1, TaskItemStatus.Cancelled, false)]
    [InlineData(0, TaskItemStatus.Pending, false)]
    public void IsOverdue_DependsOnDueDateAndStatus(int dueInDays, TaskItemStatus status, bool expected)
    {
        var task = new TaskItemBuilder().WithDueDate(Today.AddDays(dueInDays)).InStatus(status).Build();

        task.IsOverdue(Today).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(TaskItem.DueSoonDays, true)]
    [InlineData(TaskItem.DueSoonDays + 1, false)]
    [InlineData(-1, false)]
    public void IsDueSoon_WithinThreeDays_ReturnsTrue(int dueInDays, bool expected)
    {
        var task = new TaskItemBuilder().WithDueDate(Today.AddDays(dueInDays)).Build();

        task.IsDueSoon(Today).Should().Be(expected);
    }

    [Fact]
    public void CanBeEditedBy_ReporterOrAssigneeOnly()
    {
        var assignee = Guid.NewGuid();
        var task = new TaskItemBuilder().WithAssignee(assignee).Build();

        task.CanBeEditedBy(TestUsers.ReporterId).Should().BeTrue();
        task.CanBeEditedBy(assignee).Should().BeTrue();
        task.CanBeEditedBy(TestUsers.OtherUserId).Should().BeFalse();
    }
}
