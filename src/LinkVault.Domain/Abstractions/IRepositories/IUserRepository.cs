using LinkVault.Domain.Entities;

namespace LinkVault.Domain.Abstractions.IRepositories;

public interface IUserRepository : IRepository<UserEntity>
{
    Task<UserEntity?> FindByEmailBlindIndexHashAsync(
        string emailBlindIndexHash,
        CancellationToken cancellationToken = default);
}