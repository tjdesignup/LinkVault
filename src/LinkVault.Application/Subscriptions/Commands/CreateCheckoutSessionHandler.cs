using LinkVault.Application.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Subscriptions.Commands;

public class CreateCheckoutSessionHandler(
    ICurrentSubscriptionRepository subscriptionRepository,
    IStripeService stripeService,
    ICurrentUser currentUser)
    : IRequestHandler<CreateCheckoutSessionCommand, string>
{
    public async Task<string> Handle(
        CreateCheckoutSessionCommand command,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.FindByUserIdAsync(
            currentUser.UserId, cancellationToken)
            ?? throw new ResourceNotFoundException("Subscription", currentUser.UserId);

        if (subscription.IsProActive)
            throw new InvalidOperationException("User has already has an active Pro subscription.");

        return await stripeService.CreateCheckoutSessionAsync(
            subscription.StripeCustomerId,
            cancellationToken);
    }
}