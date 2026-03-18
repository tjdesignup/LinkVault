using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(AppDbContext dbContext)
    : BaseRepository<RefreshTokenEntity>(dbContext), IRefreshTokenRepository
{
    public async Task<RefreshTokenEntity?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken ct = default)
        => await Context.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

    public async Task<List<RefreshTokenEntity>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
        => await Context.RefreshTokens
        .Where(r => r.UserId == userId
            && r.IsExpired == false
            && r.IsRevoked == false)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync(ct);


    public async Task RevokeAllByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
        => await Context.RefreshTokens
            .Where(r => r.UserId == userId && r.IsRevoked == false)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsRevoked, true), ct);
}