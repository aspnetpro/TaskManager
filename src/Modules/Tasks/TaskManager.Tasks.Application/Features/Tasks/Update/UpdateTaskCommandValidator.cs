namespace TaskManager.Tasks.Application.Features.Tasks.Update;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(c => c.Title)
            .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Title cannot be empty.")
            .MaximumLength(Title.MaxLength)
            .When(c => c.Title is not null);

        RuleFor(c => c.Description)
            .MaximumLength(Description.MaxLength);

        RuleFor(c => c.Priority)
            .IsInEnum();

        RuleFor(c => c.DueDate)
            .Must(d => d is null || d >= timeProvider.GetUtcToday())
            .WithMessage("Due date cannot be in the past.");

        RuleFor(c => c.ClearDueDate)
            .Must(clear => !clear)
            .When(c => c.DueDate is not null)
            .WithMessage("Send either DueDate or ClearDueDate, not both.");
    }
}
