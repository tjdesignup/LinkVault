using LinkVault.Application.DTOs;
using LinkVault.Domain.Entities;

namespace LinkVault.Application.Mappings;

public static class FileMappingExtensions
{
    public static FileAttachmentDto ToDto(this FileAttachmentEntity attachment)
        => new(
            Id: attachment.Id,
            OriginalFileName: attachment.OriginalFileName,
            MimeType: attachment.MimeType,
            FileSizeBytes: attachment.FileSizeBytes,
            ScanStatus: attachment.ScanStatus.ToString(),
            IsAvailable: attachment.IsAvailable,
            CreatedAt: attachment.CreatedAt
        );
}