using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using NpgsqlTypes;
using LinkVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using LinkVault.Domain.Enums;

namespace LinkVault.Infrastructure.Persistence.Queries;

public sealed class LinkQueries(AppDbContext db) : ILinkQueries
{
    private readonly AppDbContext _db = db;

    public async Task<LinkDto?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var link = await _db.Links
            .AsNoTracking()
            .Where(l => l.Id == id && l.UserId == userId)
            .Select(l => new
            {
                l.Id,
                l.Url,
                l.Title,
                l.Note,
                l.Tags,
                l.OgTitle,
                l.OgDescription,
                l.OgImageUrl,
                l.MetadataStatus,
                l.CreatedAt
            })
            .FirstOrDefaultAsync(ct);


        if (link is null)
            return null;
            
        var attachments = await _db.FileAttachments
            .AsNoTracking()
            .Where(f => f.LinkId == id && !f.IsDeleted)
            .Select(f => new FileAttachmentDto(
                f.Id,
                f.OriginalFileName,
                f.MimeType,
                f.FileSizeBytes,
                f.ScanStatus.ToString(),
                f.ScanStatus == FileScanStatus.Clean,
                f.CreatedAt
            ))
            .ToListAsync(ct);
            
        return new LinkDto(
            link.Id,
            link.Url.Value,
            link.Title,
            link.Note,
            link.Tags.Select(t => t.Value).ToList(),
            link.OgTitle,
            link.OgDescription,
            link.OgImageUrl,
            link.MetadataStatus.ToString(),
            attachments,
            link.CreatedAt
            );
    }

    public async Task<PagedResultDto<LinkSummaryDto>> GetPagedAsync(
        Guid userId,
        List<string>? tags,
        string? searchTerm,
        string? cursor,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Links
            .AsNoTracking()
            .Where(l => l.UserId == userId);

        if (tags is { Count: > 0 })
        {
            foreach (var tag in tags)
            {
                var t = tag; // closure capture
                query = query.Where(l => l.Tags.Any(x => x.Value == t));
            }
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(l =>
                EF.Property<NpgsqlTsVector>(l, "SearchVector")
                    .Matches(EF.Functions.PlainToTsQuery("czech", searchTerm)));
        }

        var totalCount = await query.CountAsync(ct);

        if (!string.IsNullOrEmpty(cursor))
        {
            var (cursorDate, cursorId) = DecodeCursor(cursor);
            query = query.Where(l =>
                l.CreatedAt < cursorDate ||
                (l.CreatedAt == cursorDate && l.Id.CompareTo(cursorId) < 0));
        }

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Take(pageSize)
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

        string? nextCursor = null;
        if (items.Count == pageSize)
        {
            var last = items[^1];
            nextCursor = EncodeCursor(last.CreatedAt, last.Id);
        }

        return new PagedResultDto<LinkSummaryDto>(items, nextCursor, totalCount);
    }

    private static string EncodeCursor(DateTime createdAt, Guid id)
    {
        var raw = $"{createdAt:O}|{id}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    private static (DateTime createdAt, Guid id) DecodeCursor(string cursor)
    {
        var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = raw.Split('|');
        return (DateTime.Parse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind), Guid.Parse(parts[1]));
    }
}