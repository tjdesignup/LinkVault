using LinkVault.Domain.Enums;

namespace LinkVault.Application.DTOs;

public record StripeWebhookDto(
    string StripeSubscriptionId,
    SubscriptionEventType EventType,
    SubscriptionTier Tier,
    SubscriptionStatus Status,
    DateTime? CurrentPeriodEnd
);