using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence.Repositories;

public class FileRepository(AppDbContext dbContext)
    : BaseRepository<FileAttachmentEntity>(dbContext), IFileRepository
{
    public async Task<int> CountByLinkIdAsync(
        Guid linkId,
        CancellationToken ct = default)
        => await Context.FileAttachments
            .CountAsync(f => f.LinkId == linkId, ct);

    public async Task<List<FileAttachmentEntity>> GetByLinkIdAsync(
        Guid linkId,
        CancellationToken ct = default)
        => await Context.FileAttachments
            .Where(f => f.LinkId == linkId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);
}