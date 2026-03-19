using LinkVault.Application.DTOs;

namespace LinkVault.Application.Abstractions;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(
        string stripeCustomerId,
        CancellationToken cancellationToken = default);

    Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default);

    Task<StripeWebhookDto?> VerifyWebhookSignatureAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);
}