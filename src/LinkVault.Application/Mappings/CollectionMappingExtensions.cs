using LinkVault.Application.DTOs;
using LinkVault.Domain.Entities;

namespace LinkVault.Application.Mappings;

public static class CollectionMappingExtensions
{
    public static CollectionDto ToDto(this CollectionEntity collection)
        => new(
            Id: collection.Id,
            Name: collection.Name,
            Slug: collection.Slug.Value,
            FilterTags: collection.FilterTags.Select(t => t.Value).ToList(),
            IsPublic: collection.IsPublic,
            CreatedAt: collection.CreatedAt
        );

    public static PublicCollectionDto ToPublicDto(
        this CollectionEntity collection,
        List<LinkSummaryDto> links)
        => new(
            Name: collection.Name,
            Slug: collection.Slug.Value,
            FilterTags: collection.FilterTags.Select(t => t.Value).ToList(),
            Links: links
        );
}