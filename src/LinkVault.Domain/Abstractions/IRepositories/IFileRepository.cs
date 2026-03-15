using LinkVault.Domain.Entities;

namespace LinkVault.Domain.Abstractions.IRepositories;

public interface IFileRepository : IRepository<FileAttachmentEntity>
{
    Task<int> CountByLinkIdAsync(
        Guid linkId,
        CancellationToken cancellationToken = default);

    Task<List<FileAttachmentEntity>> GetByLinkIdAsync(
        Guid linkId,
        CancellationToken cancellationToken = default);
}