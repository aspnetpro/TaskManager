using TaskManager.Auth.Contracts;
using TaskManager.Tasks.Application.Features.Tasks.Assign;

namespace TaskManager.Tasks.UnitTests.Features.Tasks.Assign;

public sealed class AssignTaskHandlerTests
{
    private static readonly Guid Member = Guid.Parse("33333333-0000-0000-0000-000000000001");

    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly ITasksUnitOfWork _unitOfWork = Substitute.For<ITasksUnitOfWork>();
    private readonly IWorkspaceMembershipService _membership = Substitute.For<IWorkspaceMembershipService>();
    private readonly TaskItem _task = new TaskItemBuilder().Build();

    public AssignTaskHandlerTests()
    {
        _repository.GetByIdAsync(_task.Id, Arg.Any<CancellationToken>()).Returns(_task);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _membership.IsActiveMemberAsync(TestUsers.WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    private AssignTaskHandler Handler(WorkspaceRole role, Guid? userId = null) =>
        new(_repository, _unitOfWork, TestUsers.As(role, userId), _membership, TestUsers.Clock());

    [Theory]
    [InlineData(WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.TechLead)]
    public async Task Handle_LeadOrAdmin_CanAssignAnyMember(WorkspaceRole role)
    {
        var result = await Handler(role).Handle(new AssignTaskCommand(_task.Id, Member), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _task.AssigneeId.Should().Be(Member);
    }

    [Fact]
    public async Task Handle_DeveloperSelfAssign_Succeeds()
    {
        var result = await Handler(WorkspaceRole.Developer, Member).Handle(
            new AssignTaskCommand(_task.Id, Member), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeveloperAssignsSomeoneElse_ReturnsForbidden()
    {
        var result = await Handler(WorkspaceRole.Developer, TestUsers.OtherUserId).Handle(
            new AssignTaskCommand(_task.Id, Member), CancellationToken.None);

        result.Error.Code.Should().Be("Task.AssignForbidden");
        _task.AssigneeId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeveloperUnassignsSelf_Succeeds()
    {
        _task.AssignTo(Member, Member);

        var result = await Handler(WorkspaceRole.Developer, Member).Handle(
            new AssignTaskCommand(_task.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _task.AssigneeId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AssigneeNotMember_ReturnsValidationError()
    {
        _membership.IsActiveMemberAsync(TestUsers.WorkspaceId, Member, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(WorkspaceRole.Admin).Handle(new AssignTaskCommand(_task.Id, Member), CancellationToken.None);

        result.Error.Code.Should().Be("Task.AssigneeNotMember");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_Viewer_ReturnsForbidden()
    {
        var result = await Handler(WorkspaceRole.Viewer, Member).Handle(
            new AssignTaskCommand(_task.Id, Member), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }
}
