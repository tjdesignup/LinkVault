using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Enums;

namespace LinkVault.Domain.Entities;

public class SubscriptionEventEntity : BaseEntity
{
    public Guid UserId { get; private set; }
    public SubscriptionEventType EventType { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public DateTime? CurrentPeriodEnd { get; private set; }

    private SubscriptionEventEntity() { }

    private SubscriptionEventEntity(
        Guid userId,
        SubscriptionEventType eventType,
        SubscriptionTier tier,
        SubscriptionStatus status,
        string? stripeSubscriptionId,
        DateTime? currentPeriodEnd)
    {
        UserId = userId;
        EventType = eventType;
        Tier = tier;
        Status = status;
        StripeSubscriptionId = stripeSubscriptionId;
        CurrentPeriodEnd = currentPeriodEnd;
    }

    public static SubscriptionEventEntity Create(
        Guid userId,
        SubscriptionEventType eventType,
        SubscriptionTier tier,
        SubscriptionStatus status,
        string? stripeSubscriptionId,
        DateTime? currentPeriodEnd)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        return new SubscriptionEventEntity(userId, eventType, tier, status, stripeSubscriptionId, currentPeriodEnd);
    }
}