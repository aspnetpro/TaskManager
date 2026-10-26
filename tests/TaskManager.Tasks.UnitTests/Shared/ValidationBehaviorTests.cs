using FluentValidation;
using MediatR;
using TaskManager.Shared.Application.Behaviors;
using TaskManager.Tasks.Application.Features.Tasks;
using TaskManager.Tasks.Application.Features.Tasks.Create;
using TaskManager.Tasks.Application.Features.Tasks.Delete;

namespace TaskManager.Tasks.UnitTests.Shared;

/// <summary>
/// Regressão do bug do livro: o behavior original fazia (TResponse)(object)Result&lt;object&gt;
/// e explodia com InvalidCastException para qualquer Result&lt;T&gt; real.
/// </summary>
public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_InvalidGenericResultRequest_ReturnsTypedFailureWithFieldErrors()
    {
        var sut = new ValidationBehavior<CreateTaskCommand, Result<TaskResponse>>(
            [new CreateTaskCommandValidator(TestUsers.Clock())]);
        var next = Substitute.For<RequestHandlerDelegate<Result<TaskResponse>>>();

        var result = await sut.Handle(new CreateTaskCommand("", null, TaskPriority.Low), next, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>()
            .Which.Errors.Should().ContainKey(nameof(CreateTaskCommand.Title));
        await next.DidNotReceiveWithAnyArgs().Invoke(default);
    }

    [Fact]
    public async Task Handle_InvalidNonGenericResultRequest_ReturnsFailure()
    {
        var validator = new InlineValidator<DeleteTaskCommand>();
        validator.RuleFor(c => c.TaskId).NotEmpty();
        var sut = new ValidationBehavior<DeleteTaskCommand, Result>([validator]);

        var result = await sut.Handle(new DeleteTaskCommand(Guid.Empty), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        result.Error.Should().BeOfType<ValidationError>();
    }

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var sut = new ValidationBehavior<DeleteTaskCommand, Result>([]);

        var result = await sut.Handle(new DeleteTaskCommand(Guid.NewGuid()), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
