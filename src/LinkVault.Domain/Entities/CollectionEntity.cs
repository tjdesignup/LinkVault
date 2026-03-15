using LinkVault.Domain.Abstractions;
using LinkVault.Domain.ValueObjects;

namespace LinkVault.Domain.Entities;

public class CollectionEntity : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public List<Tag> FilterTags { get; private set; } = [];
    public bool IsPublic { get; private set; }

    private CollectionEntity() { }

    private CollectionEntity(Guid userId, string name, Slug slug, List<Tag> filterTags, bool isPublic)
    {
        UserId = userId;
        Name = name;
        Slug = slug;
        FilterTags = filterTags;
        IsPublic = isPublic;
    }

    public static CollectionEntity Create(Guid userId, string name, Slug slug, List<Tag> filterTags, bool isPublic)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        return new CollectionEntity(userId, name, slug, filterTags, isPublic);
    }

    public void UpdateDetails(string name, Slug slug, List<Tag> filterTags, bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = name;
        Slug = slug;
        FilterTags = filterTags;
        IsPublic = isPublic;
    }

    public void MakePublic() => IsPublic = true;

    public void MakePrivate() => IsPublic = false;

    public void Delete() => SoftDelete();
}