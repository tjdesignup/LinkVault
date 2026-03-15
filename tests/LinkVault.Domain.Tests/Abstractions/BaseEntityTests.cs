using FluentAssertions;
using LinkVault.Domain.Abstractions;

namespace LinkVault.Domain.Tests.Abstractions;

public class BaseEntityTests
{
    [Fact]
    public void BaseEntity_ShouldImplementIEntity()
    {
        var entity = new TestEntity();
        entity.Should().BeAssignableTo<IEntity>();
    }

    [Fact]
    public void BaseEntity_WhenCreated_ShouldHaveNonEmptyId()
    {
        var entity = new TestEntity();
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void BaseEntity_WhenCreated_ShouldHaveCreatedAtSet()
    {
        var before = DateTime.UtcNow;
        var entity = new TestEntity();
        var after = DateTime.UtcNow;

        entity.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void BaseEntity_WhenCreated_ShouldNotBeDeleted()
    {
        var entity = new TestEntity();

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void SoftDelete_WhenCalled_ShouldMarkEntityAsDeleted()
    {
        var entity = new TestEntity();
        var before = DateTime.UtcNow;

        entity.SoftDelete();

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void SoftDelete_WhenCalledTwice_ShouldNotChangeDeletedAt()
    {
        var entity = new TestEntity();
        entity.SoftDelete();
        var firstDeletedAt = entity.DeletedAt;

        entity.SoftDelete();

        entity.DeletedAt.Should().Be(firstDeletedAt);
    }

    private class TestEntity : BaseEntity { }
}