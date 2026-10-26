using TaskManager.Tasks.Application.Features.Tasks.Create;

namespace TaskManager.Tasks.UnitTests.Features.Tasks.Create;

public sealed class CreateTaskCommandValidatorTests
{
    private static readonly FakeTimeProvider Clock = TestUsers.Clock();
    private static readonly DateOnly Today = Clock.GetUtcToday();
    private readonly CreateTaskCommandValidator _sut = new(Clock);

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveErrors()
    {
        var result = _sut.Validate(new CreateTaskCommand("Title", "Desc", TaskPriority.Low, Today));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyTitle_ShouldHaveError(string title)
    {
        var result = _sut.Validate(new CreateTaskCommand(title, null, TaskPriority.Low));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTaskCommand.Title));
    }

    [Fact]
    public void Validate_TitleTooLong_ShouldHaveError()
    {
        var result = _sut.Validate(new CreateTaskCommand(new string('a', Title.MaxLength + 1), null, TaskPriority.Low));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTaskCommand.Title));
    }

    [Fact]
    public void Validate_DescriptionTooLong_ShouldHaveError()
    {
        var result = _sut.Validate(new CreateTaskCommand("x", new string('a', Description.MaxLength + 1), TaskPriority.Low));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTaskCommand.Description));
    }

    [Fact]
    public void Validate_UndefinedPriority_ShouldHaveError()
    {
        var result = _sut.Validate(new CreateTaskCommand("x", null, (TaskPriority)99));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTaskCommand.Priority));
    }

    [Fact]
    public void Validate_DueDateInPast_ShouldHaveError()
    {
        var result = _sut.Validate(new CreateTaskCommand("x", null, TaskPriority.Low, Today.AddDays(-1)));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTaskCommand.DueDate));
    }
}
