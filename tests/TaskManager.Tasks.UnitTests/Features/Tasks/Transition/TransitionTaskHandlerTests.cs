using TaskManager.Tasks.Application.Features.Tasks.Transition;

namespace TaskManager.Tasks.UnitTests.Features.Tasks.Transition;

public sealed class TransitionTaskHandlerTests : IDisposable
{
    private readonly TestMeterFactory _meterFactory = new();
    private readonly FakeTimeProvider _clock = TestUsers.Clock();
    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly ITasksUnitOfWork _unitOfWork = Substitute.For<ITasksUnitOfWork>();
    private readonly MetricCollector<double> _leadTime;
    private readonly TransitionTaskHandler _sut;

    public TransitionTaskHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _sut = new TransitionTaskHandler(
            _repository, _unitOfWork, TestUsers.As(WorkspaceRole.Developer, TestUsers.OtherUserId), _clock,
            new TasksMetrics(_meterFactory));
        _leadTime = new MetricCollector<double>(_meterFactory, TasksMetrics.MeterName, "taskmanager.tasks.lead_time");
    }

    public void Dispose()
    {
        _leadTime.Dispose();
        _meterFactory.Dispose();
    }

    [Fact]
    public async Task Handle_ValidTransition_SavesAndReturnsNewStatus()
    {
        var task = new TaskItemBuilder().Build();
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var result = await _sut.Handle(new TransitionTaskCommand(task.Id, TaskItemStatus.InProgress), CancellationToken.None);

        result.Value.Status.Should().Be(TaskItemStatus.InProgress);
        result.Value.AssigneeId.Should().Be(TestUsers.OtherUserId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidTransition_ReturnsBusinessRuleWithoutSaving()
    {
        var task = new TaskItemBuilder().InStatus(TaskItemStatus.Done).Build();
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var result = await _sut.Handle(new TransitionTaskCommand(task.Id, TaskItemStatus.Pending), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.BusinessRule);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_TaskNotFound_ReturnsNotFound()
    {
        var result = await _sut.Handle(new TransitionTaskCommand(Guid.NewGuid(), TaskItemStatus.Done), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_TransitionToDone_RecordsLeadTime()
    {
        var task = new TaskItemBuilder().InStatus(TaskItemStatus.InProgress).Build();
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        await _sut.Handle(new TransitionTaskCommand(task.Id, TaskItemStatus.Done), CancellationToken.None);

        var measurement = _leadTime.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        measurement.Value.Should().Be((_clock.GetUtcNow() - task.CreatedAt).TotalSeconds);
        measurement.Tags["task.priority"].Should().Be("Medium");
    }

    [Fact]
    public async Task Handle_TransitionNotToDone_RecordsNoLeadTime()
    {
        var task = new TaskItemBuilder().Build();
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        await _sut.Handle(new TransitionTaskCommand(task.Id, TaskItemStatus.InProgress), CancellationToken.None);

        _leadTime.GetMeasurementSnapshot().Should().BeEmpty();
    }
}
