namespace TaskManager.Auth.Application.Features.Auth.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().MaximumLength(User.MaxEmailLength);
        RuleFor(c => c.Password).NotEmpty().MaximumLength(PasswordRules.MaxLength);
    }
}
