using TaskManager.Tasks.Application.Features.Tasks.List;

namespace TaskManager.Tasks.UnitTests.Features.Tasks.List;

public sealed class GetTasksHandlerTests
{
    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();

    public GetTasksHandlerTests() =>
        _repository.ListAsync(Arg.Any<TaskListFilter>(), Arg.Any<CancellationToken>())
            .Returns(([new TaskItemBuilder().Build()], 41));

    [Theory]
    [InlineData(WorkspaceRole.Viewer)]
    [InlineData(WorkspaceRole.Developer)]
    [InlineData(WorkspaceRole.TechLead)]
    public async Task Handle_IncludeDeletedAsNonAdmin_ReturnsForbidden(WorkspaceRole role)
    {
        var sut = new GetTasksHandler(_repository, TestUsers.As(role), TestUsers.Clock());

        var result = await sut.Handle(new GetTasksQuery(IncludeDeleted: true), CancellationToken.None);

        result.Error.Code.Should().Be("Task.ViewDeletedForbidden");
        await _repository.DidNotReceiveWithAnyArgs().ListAsync(default!, default);
    }

    [Fact]
    public async Task Handle_OverdueFilter_PassesTodayToRepository()
    {
        var clock = TestUsers.Clock();
        var sut = new GetTasksHandler(_repository, TestUsers.As(WorkspaceRole.Developer), clock);

        await sut.Handle(new GetTasksQuery(Overdue: true, SearchTerm: "  auth  "), CancellationToken.None);

        await _repository.Received(1).ListAsync(
            Arg.Is<TaskListFilter>(f => f.OverdueOn == clock.GetUtcToday() && f.SearchTerm == "auth"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsPagingMetadata()
    {
        var sut = new GetTasksHandler(_repository, TestUsers.As(WorkspaceRole.Viewer), TestUsers.Clock());

        var result = await sut.Handle(new GetTasksQuery(Page: 2, PageSize: 20), CancellationToken.None);

        result.Value.TotalCount.Should().Be(41);
        result.Value.TotalPages.Should().Be(3);
        result.Value.Page.Should().Be(2);
        result.Value.Items.Should().HaveCount(1);
    }
}
