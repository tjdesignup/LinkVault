namespace LinkVault.Application.DTOs;

public record LinkSummaryDto(
    Guid Id,
    string Url,
    string? Title,
    List<string> Tags,
    string? OgImageUrl,
    string MetadataStatus,
    DateTime CreatedAt
);