using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Collections.Queries;

public class GetPublicCollectionHandler(
    ICollectionQueries collectionQueries)
    : IRequestHandler<GetPublicCollectionQuery, PublicCollectionDto?>
{
    public async Task<PublicCollectionDto?> Handle(
        GetPublicCollectionQuery query,
        CancellationToken cancellationToken)
        => await collectionQueries.GetPublicBySlugAsync(
            query.Slug,
            cancellationToken);
}