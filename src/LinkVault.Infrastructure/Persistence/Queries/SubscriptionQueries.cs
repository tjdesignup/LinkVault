// src/LinkVault.Infrastructure/Persistence/Queries/SubscriptionQueries.cs

using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence.Queries;

public sealed class SubscriptionQueries(AppDbContext db) : ISubscriptionQueries
{
    private readonly AppDbContext _db = db;

    public async Task<SubscriptionDto?> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        return await _db.CurrentSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new SubscriptionDto(
                s.Tier.ToString(),
                s.Status.ToString(),
                s.CurrentPeriodEnd,
                s.Tier == Domain.Enums.SubscriptionTier.Pro &&
                s.Status == Domain.Enums.SubscriptionStatus.Active
            ))
            .FirstOrDefaultAsync(ct);
    }
}