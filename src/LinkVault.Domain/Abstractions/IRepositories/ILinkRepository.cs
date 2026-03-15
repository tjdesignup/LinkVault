using LinkVault.Domain.Entities;

namespace LinkVault.Domain.Abstractions.IRepositories;

public interface ILinkRepository : IRepository<LinkEntity>
{
    Task<(List<LinkEntity> Items, string? NextCursor)> GetPagedByUserIdAsync(
        Guid userId,
        List<string>? tags,
        string? searchTerm,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}