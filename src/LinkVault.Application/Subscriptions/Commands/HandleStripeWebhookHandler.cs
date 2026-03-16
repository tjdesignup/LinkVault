using LinkVault.Application.Abstractions;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using MediatR;

namespace LinkVault.Application.Subscriptions.Commands;

public class HandleStripeWebhookHandler(
    ICurrentSubscriptionRepository subscriptionRepository,
    IStripeService stripeService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<HandleStripeWebhookCommand, Unit>
{
    public async Task<Unit> Handle(
        HandleStripeWebhookCommand command,
        CancellationToken cancellationToken)
    {
        var webhookDto = await stripeService.VerifyWebhookSignatureAsync(
            command.Payload,
            command.StripeSignature,
            cancellationToken) ?? throw new InvalidOperationException("Stripe webhook signature is invalid.");

        var subscription = await subscriptionRepository.FindByStripeSubscriptionIdAsync(
            webhookDto.StripeSubscriptionId, cancellationToken);

        if (subscription is null)
            return Unit.Value; 

        var subscriptionEvent = SubscriptionEventEntity.Create(
            subscription.UserId,
            webhookDto.EventType,
            webhookDto.Tier,
            webhookDto.Status,
            webhookDto.StripeSubscriptionId,
            webhookDto.CurrentPeriodEnd);

        subscription.ApplyEvent(subscriptionEvent);

        await subscriptionRepository.AddEventAsync(subscriptionEvent, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}