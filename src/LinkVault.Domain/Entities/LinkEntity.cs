using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Enums;
using LinkVault.Domain.ValueObjects;

namespace LinkVault.Domain.Entities;

public class LinkEntity : BaseEntity
{
    public Guid UserId { get; private set; }
    public Url Url { get; private set; } = null!;
    public string? Title { get; private set; }
    public string? Note { get; private set; }
    public List<Tag> Tags { get; private set; } = [];
    public MetadataStatus MetadataStatus { get; private set; }
    public string? OgTitle { get; private set; }
    public string? OgDescription { get; private set; }
    public string? OgImageUrl { get; private set; }

    private LinkEntity() { }

    private LinkEntity(Guid userId, Url url, string? title, string? note, List<Tag> tags)
    {
        UserId = userId;
        Url = url;
        Title = title;
        Note = note;
        Tags = tags;
        MetadataStatus = MetadataStatus.Pending;
    }

    public static LinkEntity Create(Guid userId, Url url, string? title, string? note, List<Tag> tags)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        return new LinkEntity(userId, url, title, note, tags);
    }

    public void UpdateDetails(Url url, string? title, string? note, List<Tag> tags)
    {
        Url = url;
        Title = title;
        Note = note;
        Tags = tags;
    }

    public void ApplyMetadata(string? ogTitle, string? ogDescription, string? ogImageUrl)
    {
        OgTitle = ogTitle;
        OgDescription = ogDescription;
        OgImageUrl = ogImageUrl;
        MetadataStatus = MetadataStatus.Fetched;
    }

    public void MarkMetadataFailed()
    {
        MetadataStatus = MetadataStatus.Failed;
    }

    public void Delete() => SoftDelete();
}