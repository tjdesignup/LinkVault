using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.AI.Queries;

public class SummarizeLinksHandler(
    ILinkQueries linkQueries,
    IAiSummaryService aiSummaryService,
    ICurrentUser currentUser)
    : IRequestHandler<SummarizeLinksQuery, IAsyncEnumerable<string>>
{
    public async Task<IAsyncEnumerable<string>> Handle(
        SummarizeLinksQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsProTier)
            throw new ProTierRequiredException("AI shrnutí");

        if (query.LinkIds.Count == 0)
            throw new ArgumentException("Je nutné vybrat alespoň jeden odkaz.");

        var descriptions = new List<string>();

        foreach (var linkId in query.LinkIds)
        {
            var link = await linkQueries.GetByIdAsync(
                linkId, currentUser.UserId, cancellationToken);

            if (link is null) continue;

            var description = $"Title: {link.Title ?? link.Url}\n" +
                              $"URL: {link.Url}\n" +
                              $"Note: {link.Note ?? "N/A"}\n" +
                              $"Tags: {string.Join(", ", link.Tags)}";

            descriptions.Add(description);
        }

        return aiSummaryService.SummarizeAsync(descriptions, cancellationToken);
    }
}