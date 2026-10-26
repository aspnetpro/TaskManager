using TaskManager.Tasks.Application.Features.Tasks.Update;

namespace TaskManager.Tasks.UnitTests.Features.Tasks.Update;

public sealed class UpdateTaskHandlerTests
{
    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly ITasksUnitOfWork _unitOfWork = Substitute.For<ITasksUnitOfWork>();
    private readonly TaskItem _task = new TaskItemBuilder().WithDueDate(new DateOnly(2026, 12, 1)).Build();

    public UpdateTaskHandlerTests()
    {
        _repository.GetByIdAsync(_task.Id, Arg.Any<CancellationToken>()).Returns(_task);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    private UpdateTaskHandler Handler(Guid userId) =>
        new(_repository, _unitOfWork, TestUsers.As(WorkspaceRole.Developer, userId), TestUsers.Clock());

    [Fact]
    public async Task Handle_PartialUpdate_ChangesOnlySentFields()
    {
        var command = new UpdateTaskCommand(_task.Id, _task.Version, Priority: TaskPriority.Critical);

        var result = await Handler(TestUsers.ReporterId).Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _task.Priority.Should().Be(TaskPriority.Critical);
        _task.Title.Value.Should().Be("Implement auth");
        _task.Description!.Value.Should().Be("JWT-based");
        _task.DueDate.Should().Be(new DateOnly(2026, 12, 1));
    }

    [Fact]
    public async Task Handle_EmptyDescriptionAndClearDueDate_ClearsFields()
    {
        var command = new UpdateTaskCommand(_task.Id, _task.Version, Description: "", ClearDueDate: true);

        await Handler(TestUsers.ReporterId).Handle(command, CancellationToken.None);

        _task.Description.Should().BeNull();
        _task.DueDate.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TaskNotFound_ReturnsNotFound()
    {
        var result = await Handler(TestUsers.ReporterId).Handle(new UpdateTaskCommand(Guid.NewGuid(), 0), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_UserIsNotReporterNorAssignee_ReturnsForbidden()
    {
        var result = await Handler(TestUsers.OtherUserId).Handle(
            new UpdateTaskCommand(_task.Id, _task.Version, Title: "Hijack"), CancellationToken.None);

        result.Error.Code.Should().Be("Task.EditForbidden");
        _task.Title.Value.Should().Be("Implement auth");
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_StaleVersion_ReturnsConflictWithoutChanges()
    {
        var result = await Handler(TestUsers.ReporterId).Handle(
            new UpdateTaskCommand(_task.Id, _task.Version + 1, Title: "New"), CancellationToken.None);

        result.Error.Should().Be(CommonErrors.ConcurrencyConflict);
        _task.Title.Value.Should().Be("Implement auth");
    }

    [Fact]
    public async Task Handle_ConcurrencyDetectedOnSave_ReturnsConflict()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await Handler(TestUsers.ReporterId).Handle(
            new UpdateTaskCommand(_task.Id, _task.Version, Title: "New"), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
