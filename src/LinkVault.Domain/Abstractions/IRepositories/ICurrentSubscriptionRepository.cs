using LinkVault.Domain.Entities;

namespace LinkVault.Domain.Abstractions.IRepositories;

public interface ICurrentSubscriptionRepository : IRepository<CurrentSubscriptionEntity>
{
    Task<CurrentSubscriptionEntity?> FindByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CurrentSubscriptionEntity?> FindByStripeSubscriptionIdAsync(
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default);

    Task AddEventAsync(
        SubscriptionEventEntity subscriptionEvent,
        CancellationToken cancellationToken = default);
}