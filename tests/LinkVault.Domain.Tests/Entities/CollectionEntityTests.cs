using FluentAssertions;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Entities;
using LinkVault.Domain.ValueObjects;

namespace LinkVault.Domain.Tests.Entities;

public class CollectionEntityTests
{
    private static CollectionEntity CreateCollection(
        Guid? userId = null,
        string name = "My Collection",
        Slug? slug = null,
        List<Tag>? filterTags = null,
        bool isPublic = false)
        => CollectionEntity.Create(
            userId ?? Guid.NewGuid(),
            name,
            slug ?? new Slug("my-collection"),
            filterTags ?? [],
            isPublic);

    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        var userId = Guid.NewGuid();
        var slug = new Slug("my-collection");
        var tags = new List<Tag> { new Tag("dotnet") };

        var collection = CollectionEntity.Create(userId, "My Collection", slug, tags, true);

        collection.Id.Should().NotBe(Guid.Empty);
        collection.UserId.Should().Be(userId);
        collection.Name.Should().Be("My Collection");
        collection.Slug.Should().Be(slug);
        collection.FilterTags.Should().ContainSingle(t => t.Value == "dotnet");
        collection.IsPublic.Should().BeTrue();
        collection.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldInheritFromBaseEntity()
    {
        var collection = CreateCollection();
        collection.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void Create_WhenUserIdIsEmpty_ShouldThrowArgumentException()
    {
        var act = () => CollectionEntity.Create(
            Guid.Empty, "My Collection", new Slug("my-collection"), [], false);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNameIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => CollectionEntity.Create(
            Guid.NewGuid(), value, new Slug("my-collection"), [], false);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyFilterTags_ShouldCreate()
    {
        var act = () => CreateCollection(filterTags: []);
        act.Should().NotThrow();
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateNameSlugFilterTagsAndIsPublic()
    {
        var collection = CreateCollection();
        var newSlug = new Slug("updated-collection");
        var newTags = new List<Tag> { new Tag("csharp"), new Tag("dotnet") };

        collection.UpdateDetails("Updated Collection", newSlug, newTags, true);

        collection.Name.Should().Be("Updated Collection");
        collection.Slug.Should().Be(newSlug);
        collection.FilterTags.Should().HaveCount(2);
        collection.IsPublic.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WhenNameIsEmpty_ShouldThrowArgumentException(string value)
    {
        var collection = CreateCollection();

        var act = () => collection.UpdateDetails(value, new Slug("slug"), [], false);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MakePublic_ShouldSetIsPublicTrue()
    {
        var collection = CreateCollection(isPublic: false);

        collection.MakePublic();

        collection.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void MakePrivate_ShouldSetIsPublicFalse()
    {
        var collection = CreateCollection(isPublic: true);

        collection.MakePrivate();

        collection.IsPublic.Should().BeFalse();
    }

    [Fact]
    public void Delete_ShouldMarkCollectionAsDeleted()
    {
        var collection = CreateCollection();

        collection.Delete();

        collection.IsDeleted.Should().BeTrue();
        collection.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Delete_WhenCalledTwice_ShouldNotChangeDeletedAt()
    {
        var collection = CreateCollection();
        collection.Delete();
        var firstDeletedAt = collection.DeletedAt;

        collection.Delete();

        collection.DeletedAt.Should().Be(firstDeletedAt);
    }
}