using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Subscriptions.Commands;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Subscriptions;

public class CreateCheckoutSessionHandlerTests
{
    private readonly ICurrentSubscriptionRepository _subscriptionRepository;
    private readonly IStripeService _stripeService;
    private readonly ICurrentUser _currentUser;
    private readonly CreateCheckoutSessionHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public CreateCheckoutSessionHandlerTests()
    {
        _subscriptionRepository = Substitute.For<ICurrentSubscriptionRepository>();
        _stripeService = Substitute.For<IStripeService>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new CreateCheckoutSessionHandler(
            _subscriptionRepository,
            _stripeService,
            _currentUser);

        _currentUser.UserId.Returns(_userId);
        _subscriptionRepository
            .FindByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(CreateFreeSubscription());
        _stripeService
            .CreateCheckoutSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://checkout.stripe.com/pay/cs_test_123");
    }

    [Fact]
    public async Task Handle_WhenFreeUser_ShouldReturnCheckoutUrl()
    {
        var result = await _handler.Handle(
            new CreateCheckoutSessionCommand(), CancellationToken.None);

        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("https://checkout.stripe.com");
    }

    [Fact]
    public async Task Handle_WhenFreeUser_ShouldCallStripeWithCustomerId()
    {
        await _handler.Handle(new CreateCheckoutSessionCommand(), CancellationToken.None);

        await _stripeService.Received(1).CreateCheckoutSessionAsync(
            Arg.Is<string>(id => id == "stripe-customer-123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyProTier_ShouldThrowInvalidOperationException()
    {
        _subscriptionRepository
            .FindByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(CreateProSubscription());

        var act = async () => await _handler.Handle(
            new CreateCheckoutSessionCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ShouldThrowResourceNotFoundException()
    {
        _subscriptionRepository
            .FindByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(
            new CreateCheckoutSessionCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    private static CurrentSubscriptionEntity CreateFreeSubscription()
        => CurrentSubscriptionEntity.CreateFree(Guid.NewGuid(), "stripe-customer-123");

    private static CurrentSubscriptionEntity CreateProSubscription()
    {
        var userId = Guid.NewGuid();
        var subscription = CurrentSubscriptionEntity.CreateFree(userId, "stripe-customer-123");
        subscription.ApplyEvent(SubscriptionEventEntity.Create(
            userId,
            SubscriptionEventType.Activated,
            SubscriptionTier.Pro,
            SubscriptionStatus.Active,
            "stripe-sub-123",
            DateTime.UtcNow.AddMonths(1)));
        return subscription;
    }
}