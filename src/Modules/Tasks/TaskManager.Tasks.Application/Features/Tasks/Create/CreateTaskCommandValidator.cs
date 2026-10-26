namespace TaskManager.Tasks.Application.Features.Tasks.Create;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(c => c.Title)
            .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Title is required.")
            .MaximumLength(Title.MaxLength);

        RuleFor(c => c.Description)
            .MaximumLength(Description.MaxLength);

        RuleFor(c => c.Priority)
            .IsInEnum();

        RuleFor(c => c.DueDate)
            .Must(d => d is null || d >= timeProvider.GetUtcToday())
            .WithMessage("Due date cannot be in the past.");
    }
}
