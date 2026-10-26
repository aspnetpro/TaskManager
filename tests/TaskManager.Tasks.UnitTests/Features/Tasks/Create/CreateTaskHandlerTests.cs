using TaskManager.Tasks.Application.Features.Tasks.Create;

namespace TaskManager.Tasks.UnitTests.Features.Tasks.Create;

public sealed class CreateTaskHandlerTests : IDisposable
{
    private readonly TestMeterFactory _meterFactory = new();
    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly ITasksUnitOfWork _unitOfWork = Substitute.For<ITasksUnitOfWork>();
    private readonly MetricCollector<long> _created;
    private readonly CreateTaskHandler _sut;

    public CreateTaskHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _created = new MetricCollector<long>(_meterFactory, TasksMetrics.MeterName, "taskmanager.tasks.created");
        _sut = new CreateTaskHandler(
            _repository, _unitOfWork, TestUsers.As(WorkspaceRole.Developer), TestUsers.Clock(), new TasksMetrics(_meterFactory));
    }

    public void Dispose()
    {
        _created.Dispose();
        _meterFactory.Dispose();
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithTaskResponse()
    {
        var command = new CreateTaskCommand("Implement auth", "JWT-based", TaskPriority.High);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Implement auth");
        result.Value.Description.Should().Be("JWT-based");
        result.Value.Status.Should().Be(TaskItemStatus.Pending);
        result.Value.Priority.Should().Be(TaskPriority.High);
        result.Value.ReporterId.Should().Be(TestUsers.ReporterId);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsTaskToCurrentWorkspaceAndSaves()
    {
        var command = new CreateTaskCommand("Implement auth", null, TaskPriority.Medium);

        await _sut.Handle(command, CancellationToken.None);

        _repository.Received(1).Add(Arg.Is<TaskItem>(t =>
            t.WorkspaceId == TestUsers.WorkspaceId &&
            t.ReporterId == TestUsers.ReporterId &&
            t.Description == null));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyTitle_ReturnsFailureWithoutSaving(string? title)
    {
        var command = new CreateTaskCommand(title!, null, TaskPriority.Medium);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Title.Empty");
        _repository.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_SaveFails_ReturnsSaveError()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await _sut.Handle(new CreateTaskCommand("x", null, TaskPriority.Low), CancellationToken.None);

        result.Error.Should().Be(CommonErrors.ConcurrencyConflict);
        _created.GetMeasurementSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCommand_CountsCreatedTaskByPriority()
    {
        await _sut.Handle(new CreateTaskCommand("Implement auth", null, TaskPriority.High), CancellationToken.None);

        var measurement = _created.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        measurement.Value.Should().Be(1);
        measurement.Tags["task.priority"].Should().Be("High");
    }
}
