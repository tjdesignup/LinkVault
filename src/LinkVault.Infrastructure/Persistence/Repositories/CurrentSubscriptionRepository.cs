using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence.Repositories;

public class CurrentSubscriptionRepository(AppDbContext dbContext)
    : BaseRepository<CurrentSubscriptionEntity>(dbContext), ICurrentSubscriptionRepository
{
    public async Task<CurrentSubscriptionEntity?> FindByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
        => await Context.CurrentSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task<CurrentSubscriptionEntity?> FindByStripeSubscriptionIdAsync(
        string stripeSubscriptionId,
        CancellationToken ct = default)
        => await Context.CurrentSubscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, ct);

    public async Task AddEventAsync(
        SubscriptionEventEntity subscriptionEvent,
        CancellationToken ct = default)
        => await Context.SubscriptionEvents.AddAsync(subscriptionEvent, ct);
}