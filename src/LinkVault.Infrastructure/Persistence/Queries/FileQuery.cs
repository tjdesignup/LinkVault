using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence.Queries;

public sealed class FileQueries(AppDbContext db) : IFileQueries
{
    private readonly AppDbContext _db = db;

    public async Task<FileAttachmentDto?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default)
    {
        return await _db.FileAttachments
            .AsNoTracking()
            .Where(f => f.Id == id && f.UserId == userId)
            .Select(f => new FileAttachmentDto(
                f.Id,
                f.OriginalFileName,
                f.MimeType,
                f.FileSizeBytes,
                f.ScanStatus.ToString(),
                f.ScanStatus == FileScanStatus.Clean,
                f.CreatedAt
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<(byte[] EncryptedContent, string Iv)?> GetEncryptedContentAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default)
    {
        var result = await _db.FileAttachments
            .AsNoTracking()
            .Where(f => f.Id == id && f.UserId == userId && f.ScanStatus == FileScanStatus.Clean)
            .Select(f => new { EncryptedContent = f.EncryptedContent!, EncryptionIv = f.EncryptionIv! })
            .FirstOrDefaultAsync(ct);

        if (result is null)
            return null;

        return (result.EncryptedContent, result.EncryptionIv);
    }
}