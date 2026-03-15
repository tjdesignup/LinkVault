using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Links.Queries;

public record GetLinksQuery(
    List<string>? Tags,
    string? SearchTerm,
    string? Cursor,
    int PageSize = 20
) : IRequest<PagedResultDto<LinkSummaryDto>>;