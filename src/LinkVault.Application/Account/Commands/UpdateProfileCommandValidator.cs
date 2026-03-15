using FluentValidation;

namespace LinkVault.Application.Account.Commands;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Surname)
            .NotEmpty()
            .MaximumLength(100);
    }
}