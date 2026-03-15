using MediatR;

namespace LinkVault.Application.AI.Queries;

public record SummarizeLinksQuery(
    List<Guid> LinkIds
) : IRequest<IAsyncEnumerable<string>>;