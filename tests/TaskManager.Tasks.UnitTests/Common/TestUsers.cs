namespace TaskManager.Tasks.UnitTests.Common;

public static class TestUsers
{
    public static readonly Guid WorkspaceId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    public static readonly Guid ReporterId = Guid.Parse("22222222-0000-0000-0000-000000000001");
    public static readonly Guid OtherUserId = Guid.Parse("22222222-0000-0000-0000-000000000002");

    public static ICurrentUser As(WorkspaceRole role, Guid? userId = null)
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(userId ?? ReporterId);
        user.WorkspaceId.Returns(WorkspaceId);
        user.Role.Returns(role);
        return user;
    }

    public static FakeTimeProvider Clock() =>
        new(new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero));
}
