using LinkVault.Application.DTOs;

namespace LinkVault.Application.Abstractions;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(
        string stripeCustomerId,
        CancellationToken cancellationToken = default);

    Task<StripeWebhookDto?> VerifyWebhookSignatureAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);
}