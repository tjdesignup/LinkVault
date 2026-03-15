using FluentValidation;

namespace LinkVault.Application.Links.Commands;

public class UpdateLinkCommandValidator : AbstractValidator<UpdateLinkCommand>
{
    public UpdateLinkCommandValidator()
    {
        RuleFor(x => x.LinkId)
            .NotEmpty();

        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("URL musí být platná http nebo https adresa.");

        RuleFor(x => x.Title)
            .MaximumLength(500)
            .When(x => x.Title is not null);

        RuleFor(x => x.Note)
            .MaximumLength(5000)
            .When(x => x.Note is not null);

        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= 10)
            .WithMessage("Maximálně 10 tagů na záložku.")
            .When(x => x.Tags.Count > 0);
    }
}