using LinkVault.Domain.Entities;

namespace LinkVault.Domain.Abstractions.IRepositories;

public interface ICollectionRepository : IRepository<CollectionEntity>
{
    Task<CollectionEntity?> FindBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsForUserAsync(
        Guid userId,
        string slug,
        CancellationToken cancellationToken = default);

    Task<List<CollectionEntity>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}