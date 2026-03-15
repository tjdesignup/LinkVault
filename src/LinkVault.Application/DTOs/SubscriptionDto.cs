namespace LinkVault.Application.DTOs;

public record SubscriptionDto(
    string Tier,
    string Status,
    DateTime? CurrentPeriodEnd,
    bool IsProActive
);