using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Subscriptions.Commands;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Subscriptions;

public class CancelSubscriptionHandlerTests
{
    private readonly ICurrentSubscriptionRepository _subscriptionRepository;
    private readonly IStripeService _stripeService;
    private readonly ICurrentUser _currentUser;
    private readonly CancelSubscriptionHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public CancelSubscriptionHandlerTests()
    {
        _subscriptionRepository = Substitute.For<ICurrentSubscriptionRepository>();
        _stripeService = Substitute.For<IStripeService>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new CancelSubscriptionHandler(
            _subscriptionRepository,
            _stripeService,
            _currentUser);

        _currentUser.UserId.Returns(_userId);
    }

    [Fact]
    public async Task Handle_WhenActiveProSubscription_ShouldCallStripeAndReturnUnit()
    {
        // Arrange
        var proSub = CreateProSubscription("sub_12345");
        _subscriptionRepository
            .FindByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(proSub);

        // Act
        var result = await _handler.Handle(new CancelSubscriptionCommand(), CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        await _stripeService.Received(1).CancelSubscriptionAsync(
            Arg.Is<string>(id => id == "sub_12345"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserIsFree_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _subscriptionRepository
            .FindByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(CreateFreeSubscription());

        // Act
        var act = async () => await _handler.Handle(new CancelSubscriptionCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        
        await _stripeService.DidNotReceive().CancelSubscriptionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSubscriptionIdIsNull_ShouldThrowInvalidOperationException()
    {
        var brokenSub = CreateProSubscription(null!); 
        _subscriptionRepository
            .FindByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(brokenSub);

        // Act
        var act = async () => await _handler.Handle(new CancelSubscriptionCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        _subscriptionRepository
            .FindByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var act = async () => await _handler.Handle(new CancelSubscriptionCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    private static CurrentSubscriptionEntity CreateFreeSubscription()
        => CurrentSubscriptionEntity.CreateFree(Guid.NewGuid(), "cus_123");

    private static CurrentSubscriptionEntity CreateProSubscription(string stripeSubId)
    {
        var userId = Guid.NewGuid();
        var subscription = CurrentSubscriptionEntity.CreateFree(userId, "cus_123");
        
        subscription.ApplyEvent(SubscriptionEventEntity.Create(
            userId,
            SubscriptionEventType.Activated,
            SubscriptionTier.Pro,
            SubscriptionStatus.Active,
            stripeSubId,
            DateTime.UtcNow.AddMonths(1)));
            
        return subscription;
    }
}