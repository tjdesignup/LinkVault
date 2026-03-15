using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Enums;

namespace LinkVault.Domain.Entities;

public class CurrentSubscriptionEntity : BaseEntity
{
    public Guid UserId { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public string StripeCustomerId { get; private set; } = string.Empty;
    public string? StripeSubscriptionId { get; private set; }
    public DateTime? CurrentPeriodEnd { get; private set; }

    public bool IsProActive =>
        Tier == SubscriptionTier.Pro &&
        Status == SubscriptionStatus.Active &&
        !IsDeleted;

    private CurrentSubscriptionEntity() { } 

    private CurrentSubscriptionEntity(Guid userId, string stripeCustomerId)
    {
        UserId = userId;
        Tier = SubscriptionTier.Free;
        Status = SubscriptionStatus.Active;
        StripeCustomerId = stripeCustomerId;
    }

    public static CurrentSubscriptionEntity CreateFree(Guid userId, string stripeCustomerId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(stripeCustomerId))
            throw new ArgumentException("Stripe customer ID cannot be empty.", nameof(stripeCustomerId));

        return new CurrentSubscriptionEntity(userId, stripeCustomerId);
    }

    public void ApplyEvent(SubscriptionEventEntity evt)
    {
        if (evt.UserId != UserId)
            throw new ArgumentException("Event does not belong to this subscription's user.", nameof(evt));

        Tier = evt.Tier;
        Status = evt.Status;
        StripeSubscriptionId = evt.StripeSubscriptionId;
        CurrentPeriodEnd = evt.CurrentPeriodEnd;
    }
}