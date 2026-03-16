using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Collections.Queries;

public class GetCollectionsHandler(
    ICollectionQueries collectionQueries,
    ICurrentUser currentUser)
    : IRequestHandler<GetCollectionsQuery, List<CollectionDto>>
{
    public async Task<List<CollectionDto>> Handle(
        GetCollectionsQuery query,
        CancellationToken cancellationToken)
        => await collectionQueries.GetByUserIdAsync(
            currentUser.UserId,
            cancellationToken);
}