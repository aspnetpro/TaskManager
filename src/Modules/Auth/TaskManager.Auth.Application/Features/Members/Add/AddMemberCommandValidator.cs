namespace TaskManager.Auth.Application.Features.Members.Add;

public sealed class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(User.MaxEmailLength);
        RuleFor(c => c.Role).IsInEnum();
    }
}
