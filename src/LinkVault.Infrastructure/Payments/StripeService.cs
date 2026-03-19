using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace LinkVault.Infrastructure.Payments;

public sealed class StripeService(
    string secretKey,
    string priceId,
    string successUrl,
    string cancelUrl,
    string webhookSecret) : IStripeService
{
    private readonly IStripeClient _client = new StripeClient(secretKey);
    private readonly string _priceId = priceId;
    private readonly string _successUrl = successUrl;
    private readonly string _cancelUrl = cancelUrl;
    private readonly string _webhookSecret = webhookSecret;

    public async Task<string> CreateCheckoutSessionAsync(
        string stripeCustomerId,
        CancellationToken cancellationToken = default)
    {
        var options = new SessionCreateOptions
        {
            Customer = stripeCustomerId,
            Mode = "subscription",
            LineItems =
                [
                   new() { Price = _priceId, Quantity = 1}
                ],
      
            SuccessUrl = _successUrl,
            CancelUrl = _cancelUrl
        };

        var service = new SessionService(_client);
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

        return session.Url;
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default)
    {
        var options = new SubscriptionUpdateOptions { CancelAtPeriodEnd = true };
        var service = new SubscriptionService(_client);
        await service.UpdateAsync(subscriptionId, options, cancellationToken: ct);
    }

    public Task<StripeWebhookDto?> VerifyWebhookSignatureAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);

            StripeWebhookDto? dto = stripeEvent.Type switch
            {
                EventTypes.CustomerSubscriptionCreated => MapSubscriptionEvent(stripeEvent, SubscriptionEventType.Created),
                EventTypes.CustomerSubscriptionUpdated => MapSubscriptionEvent(stripeEvent, SubscriptionEventType.Renewed),
                EventTypes.CustomerSubscriptionDeleted => MapSubscriptionEvent(stripeEvent, SubscriptionEventType.Canceled),
                EventTypes.InvoicePaymentFailed => MapSubscriptionEvent(stripeEvent, SubscriptionEventType.PastDue),
                _ => null
            };

            return Task.FromResult(dto);
        }
        catch (StripeException)
        {
            return Task.FromResult<StripeWebhookDto?>(null);
        }
    }

    private static StripeWebhookDto? MapSubscriptionEvent(Event stripeEvent, SubscriptionEventType eventType)
    {
        if (stripeEvent.Data.Object is not Subscription subscription)
            return null;

        var tier = subscription.Items.Data.Any(i => i.Price.Recurring?.Interval == "month")
            ? SubscriptionTier.Pro
            : SubscriptionTier.Free;

        var status = subscription.Status switch
        {
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "trialing" => SubscriptionStatus.Trialing,
            _ => SubscriptionStatus.Canceled
        };

        var periodEnd = subscription.Items.Data
            .Select(i => i.CurrentPeriodEnd)
            .DefaultIfEmpty()
            .Max();

        return new StripeWebhookDto(
            subscription.Id,
            eventType,
            tier,
            status,
            periodEnd == default ? null : periodEnd
        );
    }
}