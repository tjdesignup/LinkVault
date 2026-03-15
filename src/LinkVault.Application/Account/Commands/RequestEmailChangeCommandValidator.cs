using FluentValidation;

namespace LinkVault.Application.Account.Commands;

public class RequestEmailChangeCommandValidator : AbstractValidator<RequestEmailChangeCommand>
{
    public RequestEmailChangeCommandValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.CurrentPassword)
            .NotEmpty();
    }
}