using LinkVault.Application.DTOs;

namespace LinkVault.Application.Abstractions.IQueries;

public interface ICollectionQueries
{
    Task<List<CollectionDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PublicCollectionDto?> GetPublicBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}