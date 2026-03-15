using FluentAssertions;
using LinkVault.Domain.Abstractions;

namespace LinkVault.Domain.Tests.Abstractions;

public class IEntityTests
{
    [Fact]
    public void IEntity_ShouldHaveIdProperty()
    {
        var type = typeof(IEntity);
        var idProperty = type.GetProperty("Id");

        idProperty.Should().NotBeNull();
        idProperty!.PropertyType.Should().Be(typeof(Guid));
    }

    [Fact]
    public void ConcreteClass_WhenImplementsIEntity_ShouldExposeId()
    {
        var entity = new TestEntity { Id = Guid.NewGuid() };

        entity.Should().BeAssignableTo<IEntity>();
        entity.Id.Should().NotBe(Guid.Empty);
    }

    private class TestEntity : IEntity
    {
        public Guid Id { get; init; }
    }
}