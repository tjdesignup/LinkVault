// src/LinkVault.Infrastructure/Persistence/Queries/CollectionQueries.cs

using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence.Queries;

public sealed class CollectionQueries(AppDbContext db) : ICollectionQueries
{
    private readonly AppDbContext _db = db;

    public async Task<List<CollectionDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        return await _db.Collections
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new CollectionDto(
                c.Id,
                c.Name,
                c.Slug.Value,
                c.FilterTags.Select(t => t.Value).ToList(),
                c.IsPublic,
                c.CreatedAt
            ))
            .ToListAsync(ct);
    }

    public async Task<PublicCollectionDto?> GetPublicBySlugAsync(
        string slug,
        CancellationToken ct = default)
    {
        var collection = await _db.Collections
            .AsNoTracking()
            .Where(c => c.Slug.Value == slug && c.IsPublic)
            .Select(c => new
            {
                c.Name,
                c.Slug,
                FilterTags = c.FilterTags.Select(t => t.Value).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (collection is null)
            return null;

        var links = await _db.Links
            .AsNoTracking()
            .Where(l => l.Tags.Any(t => collection.FilterTags.Contains(t.Value)))
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new LinkSummaryDto(
                l.Id,
                l.Url.Value,
                l.Title,
                l.Tags.Select(t => t.Value).ToList(),
                l.OgImageUrl,
                l.MetadataStatus.ToString(),
                l.CreatedAt
            ))
            .ToListAsync(ct);

        return new PublicCollectionDto(
            collection.Name,
            collection.Slug.Value,
            collection.FilterTags,
            links
        );
    }
}