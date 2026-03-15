namespace LinkVault.Application.Abstractions;

public interface IAiSummaryService
{
    IAsyncEnumerable<string> SummarizeAsync(
        IEnumerable<string> linkDescriptions,
        CancellationToken cancellationToken = default);
}