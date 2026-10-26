namespace TaskManager.Tasks.Application.Features.Tasks.Transition;

public sealed class TransitionTaskCommandValidator : AbstractValidator<TransitionTaskCommand>
{
    public TransitionTaskCommandValidator()
    {
        RuleFor(c => c.NewStatus).IsInEnum();
    }
}
