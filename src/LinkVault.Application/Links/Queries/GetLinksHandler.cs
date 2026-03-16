using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Links.Queries;

public class GetLinksHandler(
    ILinkQueries linkQueries,
    ICurrentUser currentUser)
    : IRequestHandler<GetLinksQuery, PagedResultDto<LinkSummaryDto>>
{
    public async Task<PagedResultDto<LinkSummaryDto>> Handle(
        GetLinksQuery query,
        CancellationToken cancellationToken)
        => await linkQueries.GetPagedAsync(
            currentUser.UserId,
            query.Tags,
            query.SearchTerm,
            query.Cursor,
            query.PageSize,
            cancellationToken);
}