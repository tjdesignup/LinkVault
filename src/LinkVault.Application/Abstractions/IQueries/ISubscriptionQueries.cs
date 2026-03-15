using LinkVault.Application.DTOs;

namespace LinkVault.Application.Abstractions.IQueries;

public interface ISubscriptionQueries
{
    Task<SubscriptionDto?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}