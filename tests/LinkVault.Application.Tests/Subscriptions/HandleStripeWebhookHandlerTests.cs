using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Application.Subscriptions.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using MediatR;
using NSubstitute;

namespace LinkVault.Application.Tests.Subscriptions;

public class HandleStripeWebhookHandlerTests
{
    private readonly ICurrentSubscriptionRepository _subscriptionRepository;
    private readonly IStripeService _stripeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HandleStripeWebhookHandler _handler;

    public HandleStripeWebhookHandlerTests()
    {
        _subscriptionRepository = Substitute.For<ICurrentSubscriptionRepository>();
        _stripeService = Substitute.For<IStripeService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new HandleStripeWebhookHandler(
            _subscriptionRepository,
            _stripeService,
            _unitOfWork);

        _stripeService
            .VerifyWebhookSignatureAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateStripeWebhookDto(SubscriptionEventType.Activated));

        _subscriptionRepository
            .FindByStripeSubscriptionIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateFreeSubscription());
    }

    private static HandleStripeWebhookCommand ValidCommand() => new(
        Payload: "raw-stripe-payload",
        StripeSignature: "stripe-signature");

    [Fact]
    public async Task Handle_WhenValidWebhook_ShouldReturnUnit()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Handle_WhenActivatedEvent_ShouldApplyEventToSubscription()
    {
        CurrentSubscriptionEntity? capturedSubscription = null;
        _subscriptionRepository
            .FindByStripeSubscriptionIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedSubscription = CreateFreeSubscription();
                return capturedSubscription;
            });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedSubscription!.Tier.Should().Be(SubscriptionTier.Pro);
        capturedSubscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task Handle_WhenValidWebhook_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidWebhook_ShouldAddSubscriptionEvent()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _subscriptionRepository.Received(1).AddEventAsync(
            Arg.Any<SubscriptionEventEntity>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenInvalidSignature_ShouldThrowInvalidOperationException()
    {
        _stripeService
            .VerifyWebhookSignatureAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((StripeWebhookDto?)null);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenInvalidSignature_ShouldNotSaveChanges()
    {
        _stripeService
            .VerifyWebhookSignatureAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((StripeWebhookDto?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static CurrentSubscriptionEntity CreateFreeSubscription()
    {
        var userId = Guid.NewGuid();
        return CurrentSubscriptionEntity.CreateFree(userId, "stripe-customer-123");
    }

    private static StripeWebhookDto CreateStripeWebhookDto(SubscriptionEventType eventType)
        => new(
            StripeSubscriptionId: "stripe-sub-123",
            EventType: eventType,
            Tier: SubscriptionTier.Pro,
            Status: SubscriptionStatus.Active,
            CurrentPeriodEnd: DateTime.UtcNow.AddMonths(1));
}