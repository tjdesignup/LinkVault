using LinkVault.Application.DTOs;
using LinkVault.Domain.Entities;

namespace LinkVault.Application.Mappings;

public static class LinkMappingExtensions
{
    public static LinkDto ToDto(this LinkEntity link, List<FileAttachmentDto> attachments)
        => new(
            Id: link.Id,
            Url: link.Url.Value,
            Title: link.Title,
            Note: link.Note,
            Tags: link.Tags.Select(t => t.Value).ToList(),
            OgTitle: link.OgTitle,
            OgDescription: link.OgDescription,
            OgImageUrl: link.OgImageUrl,
            MetadataStatus: link.MetadataStatus.ToString(),
            Attachments: attachments,
            CreatedAt: link.CreatedAt
        );

    public static LinkSummaryDto ToSummaryDto(this LinkEntity link)
        => new(
            Id: link.Id,
            Url: link.Url.Value,
            Title: link.Title,
            Tags: link.Tags.Select(t => t.Value).ToList(),
            OgImageUrl: link.OgImageUrl,
            MetadataStatus: link.MetadataStatus.ToString(),
            CreatedAt: link.CreatedAt
        );
}