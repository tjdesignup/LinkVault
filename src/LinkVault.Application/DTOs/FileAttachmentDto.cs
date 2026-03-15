namespace LinkVault.Application.DTOs;

public record FileAttachmentDto(
    Guid Id,
    string OriginalFileName,
    string MimeType,
    long FileSizeBytes,
    string ScanStatus,
    bool IsAvailable,
    DateTime CreatedAt
);