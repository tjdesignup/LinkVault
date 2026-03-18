using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence.Repositories;

public class CollectionRepository(AppDbContext dbContext)
    : BaseRepository<CollectionEntity>(dbContext), ICollectionRepository
{
    public async Task<CollectionEntity?> FindBySlugAsync(
        string slug,
        CancellationToken ct = default)
        => await Context.Collections
            .FirstOrDefaultAsync(c => c.Slug.Value == slug && c.IsPublic, ct);

    public async Task<bool> SlugExistsForUserAsync(
        Guid userId,
        string slug,
        CancellationToken ct = default)
        => await Context.Collections
            .AnyAsync(c => c.UserId == userId && c.Slug.Value == slug, ct);

    public async Task<List<CollectionEntity>> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
        => await Context.Collections
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
}