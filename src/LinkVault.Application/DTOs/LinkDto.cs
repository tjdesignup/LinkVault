
namespace LinkVault.Application.DTOs;

public record LinkDto(
    Guid Id,
    string Url,
    string? Title,
    string? Note,
    List<string> Tags,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string MetadataStatus,
    List<FileAttachmentDto> Attachments,
    DateTime CreatedAt
);