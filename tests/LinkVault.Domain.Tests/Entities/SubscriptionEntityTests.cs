using FluentAssertions;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;

namespace LinkVault.Domain.Tests.Entities;

public class SubscriptionEventEntityTests
{

    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        var userId = Guid.NewGuid();

        var evt = SubscriptionEventEntity.Create(
            userId,
            SubscriptionEventType.Activated,
            SubscriptionTier.Pro,
            SubscriptionStatus.Active,
            "stripe-sub-123",
            DateTime.UtcNow.AddMonths(1));

        evt.Id.Should().NotBe(Guid.Empty);
        evt.UserId.Should().Be(userId);
        evt.EventType.Should().Be(SubscriptionEventType.Activated);
        evt.Tier.Should().Be(SubscriptionTier.Pro);
        evt.Status.Should().Be(SubscriptionStatus.Active);
        evt.StripeSubscriptionId.Should().Be("stripe-sub-123");
        evt.CurrentPeriodEnd.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldInheritFromBaseEntity()
    {
        var evt = SubscriptionEventEntity.Create(
            Guid.NewGuid(),
            SubscriptionEventType.Created,
            SubscriptionTier.Free,
            SubscriptionStatus.Active,
            null,
            null);

        evt.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void Create_WhenUserIdIsEmpty_ShouldThrowArgumentException()
    {
        var act = () => SubscriptionEventEntity.Create(
            Guid.Empty,
            SubscriptionEventType.Created,
            SubscriptionTier.Free,
            SubscriptionStatus.Active,
            null,
            null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullStripeIdAndPeriodEnd_ShouldCreate()
    {
        var act = () => SubscriptionEventEntity.Create(
            Guid.NewGuid(),
            SubscriptionEventType.Created,
            SubscriptionTier.Free,
            SubscriptionStatus.Active,
            null,
            null);

        act.Should().NotThrow();
    }
}

public class CurrentSubscriptionEntityTests
{
    private static CurrentSubscriptionEntity CreateFreeSubscription(Guid? userId = null)
        => CurrentSubscriptionEntity.CreateFree(userId ?? Guid.NewGuid(), "stripe-customer-123");

    [Fact]
    public void CreateFree_ShouldInitializePropertiesCorrectly()
    {
        var userId = Guid.NewGuid();

        var subscription = CurrentSubscriptionEntity.CreateFree(userId, "stripe-customer-123");

        subscription.Id.Should().NotBe(Guid.Empty);
        subscription.UserId.Should().Be(userId);
        subscription.Tier.Should().Be(SubscriptionTier.Free);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.StripeCustomerId.Should().Be("stripe-customer-123");
        subscription.StripeSubscriptionId.Should().BeNull();
        subscription.CurrentPeriodEnd.Should().BeNull();
        subscription.IsProActive.Should().BeFalse();
    }

    [Fact]
    public void CreateFree_ShouldInheritFromBaseEntity()
    {
        var subscription = CreateFreeSubscription();
        subscription.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void CreateFree_WhenUserIdIsEmpty_ShouldThrowArgumentException()
    {
        var act = () => CurrentSubscriptionEntity.CreateFree(Guid.Empty, "stripe-customer-123");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFree_WhenStripeCustomerIdIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => CurrentSubscriptionEntity.CreateFree(Guid.NewGuid(), value);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsProActive_WhenFree_ShouldReturnFalse()
    {
        var subscription = CreateFreeSubscription();
        subscription.IsProActive.Should().BeFalse();
    }

    [Fact]
    public void IsProActive_WhenProAndActive_ShouldReturnTrue()
    {
        var subscription = CreateFreeSubscription();

        subscription.ApplyEvent(SubscriptionEventEntity.Create(
            subscription.UserId,
            SubscriptionEventType.Activated,
            SubscriptionTier.Pro,
            SubscriptionStatus.Active,
            "stripe-sub-123",
            DateTime.UtcNow.AddMonths(1)));

        subscription.IsProActive.Should().BeTrue();
    }

    [Fact]
    public void IsProActive_WhenProButCanceled_ShouldReturnFalse()
    {
        var subscription = CreateFreeSubscription();

        subscription.ApplyEvent(SubscriptionEventEntity.Create(
            subscription.UserId,
            SubscriptionEventType.Activated,
            SubscriptionTier.Pro,
            SubscriptionStatus.Active,
            "stripe-sub-123",
            DateTime.UtcNow.AddMonths(1)));

        subscription.ApplyEvent(SubscriptionEventEntity.Create(
            subscription.UserId,
            SubscriptionEventType.Canceled,
            SubscriptionTier.Free,
            SubscriptionStatus.Canceled,
            null,
            null));

        subscription.IsProActive.Should().BeFalse();
    }

    [Fact]
    public void IsProActive_WhenProButDeleted_ShouldReturnFalse()
    {
        var subscription = CreateFreeSubscription();

        subscription.ApplyEvent(SubscriptionEventEntity.Create(
            subscription.UserId,
            SubscriptionEventType.Activated,
            SubscriptionTier.Pro,
            SubscriptionStatus.Active,
            "stripe-sub-123",
            DateTime.UtcNow.AddMonths(1)));

        subscription.SoftDelete();

        subscription.IsProActive.Should().BeFalse();
    }

    [Fact]
    public void ApplyEvent_ShouldUpdateAllFields()
    {
        var subscription = CreateFreeSubscription();
        var periodEnd = DateTime.UtcNow.AddMonths(1);

        subscription.ApplyEvent(SubscriptionEventEntity.Create(
            subscription.UserId,
            SubscriptionEventType.Activated,
            SubscriptionTier.Pro,
            SubscriptionStatus.Active,
            "stripe-sub-123",
            periodEnd));

        subscription.Tier.Should().Be(SubscriptionTier.Pro);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.StripeSubscriptionId.Should().Be("stripe-sub-123");
        subscription.CurrentPeriodEnd.Should().BeCloseTo(periodEnd, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ApplyEvent_WhenEventFromDifferentUser_ShouldThrowArgumentException()
    {
        var subscription = CreateFreeSubscription();

        var act = () => subscription.ApplyEvent(SubscriptionEventEntity.Create(
            Guid.NewGuid(), 
            SubscriptionEventType.Activated,
            SubscriptionTier.Pro,
            SubscriptionStatus.Active,
            "stripe-sub-123",
            DateTime.UtcNow.AddMonths(1)));

        act.Should().Throw<ArgumentException>();
    }
}