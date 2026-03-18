using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence.Repositories;

public class LinkRepository(AppDbContext dbContext)
    : BaseRepository<LinkEntity>(dbContext), ILinkRepository
{
    public async Task<(List<LinkEntity> Items, string? NextCursor)> GetPagedByUserIdAsync(
    Guid userId,
    List<string>? tags,
    string? searchTerm,
    string? cursor,
    int pageSize,
    CancellationToken ct = default)
    {
        var query = Context.Links
            .Where(l => l.UserId == userId);

        if (tags is { Count: > 0 })
            query = query.Where(l => l.Tags.Any(t => tags.Contains(t.Value)));

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(l =>
                EF.Functions.ToTsVector("czech",
                    (l.Title ?? "") + " " + (l.OgTitle ?? ""))
                .Matches(EF.Functions.ToTsQuery("czech", searchTerm)));

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            var (cursorDate, cursorId) = DecodeCursor(cursor);
            query = query.Where(l =>
                l.CreatedAt < cursorDate ||
                (l.CreatedAt == cursorDate && l.Id.CompareTo(cursorId) < 0));
        }

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Take(pageSize + 1)
            .ToListAsync(ct);

        string? nextCursor = null;
        if (items.Count > pageSize)
        {
            items.RemoveAt(items.Count - 1); 
            var last = items[^1];
            nextCursor = EncodeCursor(last.CreatedAt, last.Id);
        }

        return (items, nextCursor);
    }

    private static string EncodeCursor(DateTime createdAt, Guid id)
    {
        var raw = $"{createdAt:O}|{id}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    private static (DateTime CreatedAt, Guid Id) DecodeCursor(string cursor)
    {
        var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = raw.Split('|');
        return (DateTime.Parse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind), Guid.Parse(parts[1]));
    }

    public async Task<int> CountByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
        => await Context.Links
            .CountAsync(l => l.UserId == userId, ct);
}