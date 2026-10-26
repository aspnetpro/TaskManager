namespace TaskManager.Notifications.UnitTests.EventHandlers;

public sealed class NotificationHandlerFixture
{
    public static readonly Guid WorkspaceId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    public static readonly Guid Reporter = Guid.Parse("22222222-0000-0000-0000-000000000001");
    public static readonly Guid Assignee = Guid.Parse("22222222-0000-0000-0000-000000000002");
    public static readonly Guid Lead = Guid.Parse("22222222-0000-0000-0000-000000000003");

    public NotificationHandlerFixture() =>
        UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero));
    public INotificationRepository Repository { get; } = Substitute.For<INotificationRepository>();
    public INotificationsUnitOfWork UnitOfWork { get; } = Substitute.For<INotificationsUnitOfWork>();

    public NotificationWriter Writer() => new(Repository, UnitOfWork);

    /// <summary>Notificações passadas ao AddRange (todas as chamadas).</summary>
    public List<Notification> Written() =>
        Repository.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(INotificationRepository.AddRange))
            .SelectMany(c => (IEnumerable<Notification>)c.GetArguments()[0]!)
            .ToList();

    public static TaskAssignedIntegrationEvent Assigned(Guid? assignee, Guid assignedBy) =>
        new(Guid.NewGuid(), DateTimeOffset.UnixEpoch, WorkspaceId, Guid.NewGuid(), "Review PR", null, assignee, assignedBy);

    public static TaskStatusChangedIntegrationEvent StatusChanged(Guid changedBy, Guid? assignee) =>
        new(Guid.NewGuid(), DateTimeOffset.UnixEpoch, WorkspaceId, Guid.NewGuid(), "Review PR", "Pending", "InProgress",
            changedBy, Reporter, assignee);
}
