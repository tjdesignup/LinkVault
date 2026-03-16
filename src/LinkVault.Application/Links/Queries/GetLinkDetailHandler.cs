using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Links.Queries;

public class GetLinkDetailHandler(
    ILinkQueries linkQueries,
    ICurrentUser currentUser)
    : IRequestHandler<GetLinkDetailQuery, LinkDto?>
{
    public async Task<LinkDto?> Handle(
        GetLinkDetailQuery query,
        CancellationToken cancellationToken)
        => await linkQueries.GetByIdAsync(
            query.LinkId,
            currentUser.UserId,
            cancellationToken);
}