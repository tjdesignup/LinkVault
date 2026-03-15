using FluentValidation;

namespace LinkVault.Application.Account.Commands;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches("[A-Z]").WithMessage("Heslo musí obsahovat alespoň jedno velké písmeno.")
            .Matches("[0-9]").WithMessage("Heslo musí obsahovat alespoň jednu číslici.");

        RuleFor(x => x.NewPasswordConfirmation)
            .NotEmpty()
            .Equal(x => x.NewPassword).WithMessage("Potvrzení hesla se musí shodovat s novým heslem.");
    }
}