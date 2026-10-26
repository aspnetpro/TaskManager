namespace TaskManager.Auth.Application.Features.Auth.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(User.MaxEmailLength);
        RuleFor(c => c.Password).NotEmpty().Length(PasswordRules.MinLength, PasswordRules.MaxLength);
        RuleFor(c => c.FullName)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Full name is required.")
            .MaximumLength(User.MaxFullNameLength);
        RuleFor(c => c.WorkspaceName)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Workspace name is required.")
            .MaximumLength(Workspace.MaxNameLength);
    }
}
