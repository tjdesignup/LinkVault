using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext dbContext) : BaseRepository<UserEntity>(dbContext),IUserRepository
{
    public async Task<UserEntity?> FindByEmailBlindIndexHashAsync(
        string emailBlindIndexHash,
        CancellationToken ct = default)
        => await Context.Users
            .FirstOrDefaultAsync(u => u.EmailBlindIndexHash == emailBlindIndexHash, ct);

    public async Task<bool> ExistsByEmailBlindIndexAsync(
        string emailBlindIndexHash,
        CancellationToken ct = default)
        => await Context.Users
            .AnyAsync(u => u.EmailBlindIndexHash == emailBlindIndexHash, ct);
}