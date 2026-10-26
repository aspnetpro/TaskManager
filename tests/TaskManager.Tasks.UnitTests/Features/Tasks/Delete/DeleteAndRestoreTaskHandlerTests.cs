using TaskManager.Tasks.Application.Features.Tasks.Delete;
using TaskManager.Tasks.Application.Features.Tasks.Restore;

namespace TaskManager.Tasks.UnitTests.Features.Tasks.Delete;

public sealed class DeleteAndRestoreTaskHandlerTests
{
    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly ITasksUnitOfWork _unitOfWork = Substitute.For<ITasksUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestUsers.Clock();
    private readonly TaskItem _task = new TaskItemBuilder().Build();

    public DeleteAndRestoreTaskHandlerTests()
    {
        _repository.GetByIdAsync(_task.Id, Arg.Any<CancellationToken>()).Returns(_task);
        _repository.GetDeletedByIdAsync(_task.Id, Arg.Any<CancellationToken>()).Returns(_task);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    [Theory]
    [InlineData(WorkspaceRole.Viewer)]
    [InlineData(WorkspaceRole.Developer)]
    [InlineData(WorkspaceRole.TechLead)]
    public async Task Delete_NonAdmin_ReturnsForbidden(WorkspaceRole role)
    {
        var sut = new DeleteTaskHandler(_repository, _unitOfWork, TestUsers.As(role), _clock);

        var result = await sut.Handle(new DeleteTaskCommand(_task.Id), CancellationToken.None);

        result.Error.Code.Should().Be("Task.DeleteForbidden");
        _task.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_Admin_SoftDeletesAndSaves()
    {
        var sut = new DeleteTaskHandler(_repository, _unitOfWork, TestUsers.As(WorkspaceRole.Admin), _clock);

        var result = await sut.Handle(new DeleteTaskCommand(_task.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _task.IsDeleted.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restore_Admin_WithinWindow_Restores()
    {
        _task.Delete(TestUsers.ReporterId, _clock.GetUtcNow());
        _clock.Advance(TimeSpan.FromDays(10));
        var sut = new RestoreTaskHandler(_repository, _unitOfWork, TestUsers.As(WorkspaceRole.Admin), _clock);

        var result = await sut.Handle(new RestoreTaskCommand(_task.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Restore_AfterWindow_ReturnsBusinessRule()
    {
        _task.Delete(TestUsers.ReporterId, _clock.GetUtcNow());
        _clock.Advance(TimeSpan.FromDays(TaskItem.RestoreWindowDays + 1));
        var sut = new RestoreTaskHandler(_repository, _unitOfWork, TestUsers.As(WorkspaceRole.Admin), _clock);

        var result = await sut.Handle(new RestoreTaskCommand(_task.Id), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.BusinessRule);
    }
}
