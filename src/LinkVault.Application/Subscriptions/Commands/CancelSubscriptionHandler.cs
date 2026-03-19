using LinkVault.Application.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Subscriptions.Commands;

public class CancelSubscriptionHandler(
    ICurrentSubscriptionRepository subscriptionRepository,
    IStripeService stripeService,
    ICurrentUser currentUser)
    : IRequestHandler<CancelSubscriptionCommand, Unit>
{
    public async Task<Unit> Handle(
        CancelSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.FindByUserIdAsync(
            currentUser.UserId, cancellationToken)
            ?? throw new ResourceNotFoundException("Subscription", currentUser.UserId);

        if (string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            throw new InvalidOperationException("User has not an active Pro subscription.");
        }

        if (!subscription.IsProActive)
            throw new InvalidOperationException("User has already cancel an active Pro subscription.");

        await stripeService.CancelSubscriptionAsync(
            subscription.StripeSubscriptionId,
            cancellationToken);

        return Unit.Value;
    }
}