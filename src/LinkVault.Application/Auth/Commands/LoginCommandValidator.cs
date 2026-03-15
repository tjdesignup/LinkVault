using FluentValidation;

namespace LinkVault.Application.Auth.Commands;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();

        RuleFor(x => x.DeviceName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.IpAddress)
            .NotEmpty();
    }
}