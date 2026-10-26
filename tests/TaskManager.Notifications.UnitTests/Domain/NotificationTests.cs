namespace TaskManager.Notifications.UnitTests.Domain;

public sealed class NotificationTests
{
    [Fact]
    public void MarkRead_Twice_KeepsFirstReadDate()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.TaskAssigned,
            Guid.NewGuid(), "Title", Guid.NewGuid(), null, Guid.NewGuid(), DateTimeOffset.UnixEpoch);
        var first = DateTimeOffset.UnixEpoch.AddHours(1);

        notification.MarkRead(first);
        notification.MarkRead(first.AddHours(1));

        notification.ReadAt.Should().Be(first);
    }

    [Fact]
    public void Create_LongTitle_IsTruncatedToColumnSize()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.TaskAssigned,
            Guid.NewGuid(), new string('x', 500), Guid.NewGuid(), null, Guid.NewGuid(), DateTimeOffset.UnixEpoch);

        notification.TaskTitle.Should().HaveLength(Notification.MaxTaskTitleLength);
    }
}
