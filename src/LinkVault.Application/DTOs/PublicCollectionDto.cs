namespace LinkVault.Application.DTOs;

public record PublicCollectionDto(
    string Name,
    string Slug,
    List<string> FilterTags,
    List<LinkSummaryDto> Links
);