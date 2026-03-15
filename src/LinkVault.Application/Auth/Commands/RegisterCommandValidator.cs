using FluentValidation;

namespace LinkVault.Application.Auth.Commands;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches("[A-Z]").WithMessage("Heslo musí obsahovat alespoň jedno velké písmeno.")
            .Matches("[0-9]").WithMessage("Heslo musí obsahovat alespoň jednu číslici.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Surname)
            .NotEmpty()
            .MaximumLength(100);
    }
}