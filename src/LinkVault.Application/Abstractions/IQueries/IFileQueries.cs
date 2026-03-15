using LinkVault.Application.DTOs;

namespace LinkVault.Application.Abstractions.IQueries;

public interface IFileQueries
{
    Task<FileAttachmentDto?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<(byte[] EncryptedContent, string Iv)?> GetEncryptedContentAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}