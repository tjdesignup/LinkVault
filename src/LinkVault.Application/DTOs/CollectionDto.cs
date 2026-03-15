namespace LinkVault.Application.DTOs;

public record CollectionDto(
    Guid Id,
    string Name,
    string Slug,
    List<string> FilterTags,
    bool IsPublic,
    DateTime CreatedAt
);