using FluentValidation;

namespace LinkVault.Application.Collections.Commands;

public class CreateCollectionCommandValidator : AbstractValidator<CreateCollectionCommand>
{
    public CreateCollectionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.FilterTags)
            .Must(tags => tags.Count <= 10)
            .WithMessage("Maximálně 10 tagů na kolekci.")
            .When(x => x.FilterTags.Count > 0);
    }
}