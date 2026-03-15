using LinkVault.Domain.Entities;

namespace LinkVault.Domain.Abstractions.IRepositories;

public interface IRefreshTokenRepository : IRepository<RefreshTokenEntity>
{
    Task<RefreshTokenEntity?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task RevokeAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<RefreshTokenEntity>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}