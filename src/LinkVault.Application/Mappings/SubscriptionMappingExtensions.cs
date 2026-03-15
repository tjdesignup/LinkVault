using LinkVault.Application.DTOs;
using LinkVault.Domain.Entities;

namespace LinkVault.Application.Mappings;

public static class SubscriptionMappingExtensions
{
    public static SubscriptionDto ToDto(this CurrentSubscriptionEntity subscription)
        => new(
            Tier: subscription.Tier.ToString(),
            Status: subscription.Status.ToString(),
            CurrentPeriodEnd: subscription.CurrentPeriodEnd,
            IsProActive: subscription.IsProActive
        );
}