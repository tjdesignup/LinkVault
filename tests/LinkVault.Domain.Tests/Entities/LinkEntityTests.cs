using FluentAssertions;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;
using LinkVault.Domain.ValueObjects;

namespace LinkVault.Domain.Tests.Entities;

public class LinkEntityTests
{
    private static LinkEntity CreateLink(
        Guid? userId = null,
        string url = "https://example.com",
        string? title = null,
        string? note = null,
        List<Tag>? tags = null)
        => LinkEntity.Create(userId ?? Guid.NewGuid(), new Url(url), title, note, tags ?? []);

    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        var userId = Guid.NewGuid();
        var url = new Url("https://example.com");
        var tags = new List<Tag> { new Tag("dotnet") };

        var link = LinkEntity.Create(userId, url, "My Title", "My note", tags);

        link.Id.Should().NotBe(Guid.Empty);
        link.UserId.Should().Be(userId);
        link.Url.Should().Be(url);
        link.Title.Should().Be("My Title");
        link.Note.Should().Be("My note");
        link.Tags.Should().ContainSingle(t => t.Value == "dotnet");
        link.MetadataStatus.Should().Be(MetadataStatus.Pending);
        link.IsDeleted.Should().BeFalse();
        link.OgTitle.Should().BeNull();
        link.OgDescription.Should().BeNull();
        link.OgImageUrl.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInheritFromBaseEntity()
    {
        var link = CreateLink();
        link.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void Create_WhenUserIdIsEmpty_ShouldThrowArgumentException()
    {
        var act = () => LinkEntity.Create(Guid.Empty, new Url("https://example.com"), null, null, []);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullTitleAndNote_ShouldCreate()
    {
        var act = () => CreateLink(title: null, note: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_WithEmptyTags_ShouldCreate()
    {
        var link = CreateLink(tags: []);
        link.Tags.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateUrlTitleNoteTags()
    {
        var link = CreateLink();
        var newUrl = new Url("https://updated.com");
        var newTags = new List<Tag> { new("csharp") };

        link.UpdateDetails(newUrl, "Updated Title", "Updated note", newTags);

        link.Url.Should().Be(newUrl);
        link.Title.Should().Be("Updated Title");
        link.Note.Should().Be("Updated note");
        link.Tags.Should().ContainSingle(t => t.Value == "csharp");
    }

    [Fact]
    public void UpdateDetails_WithNullTitleAndNote_ShouldClearThem()
    {
        var link = CreateLink(title: "Old Title", note: "Old Note");

        link.UpdateDetails(new Url("https://example.com"), null, null, []);

        link.Title.Should().BeNull();
        link.Note.Should().BeNull();
    }

    [Fact]
    public void ApplyMetadata_ShouldUpdateOgFieldsAndSetStatusFetched()
    {
        var link = CreateLink();

        link.ApplyMetadata("OG Title", "OG Description", "https://example.com/image.png");

        link.OgTitle.Should().Be("OG Title");
        link.OgDescription.Should().Be("OG Description");
        link.OgImageUrl.Should().Be("https://example.com/image.png");
        link.MetadataStatus.Should().Be(MetadataStatus.Fetched);
    }

    [Fact]
    public void ApplyMetadata_WithNullValues_ShouldStillSetStatusFetched()
    {
        var link = CreateLink();

        link.ApplyMetadata(null, null, null);

        link.MetadataStatus.Should().Be(MetadataStatus.Fetched);
    }

    [Fact]
    public void MarkMetadataFailed_ShouldSetStatusFailed()
    {
        var link = CreateLink();

        link.MarkMetadataFailed();

        link.MetadataStatus.Should().Be(MetadataStatus.Failed);
        link.OgTitle.Should().BeNull();
        link.OgDescription.Should().BeNull();
        link.OgImageUrl.Should().BeNull();
    }

    [Fact]
    public void Delete_ShouldMarkLinkAsDeleted()
    {
        var link = CreateLink();

        link.Delete();

        link.IsDeleted.Should().BeTrue();
        link.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Delete_WhenCalledTwice_ShouldNotChangeDeletedAt()
    {
        var link = CreateLink();
        link.Delete();
        var firstDeletedAt = link.DeletedAt;

        link.Delete();

        link.DeletedAt.Should().Be(firstDeletedAt);
    }
}