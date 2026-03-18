using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using LinkVault.Domain.Enums;

namespace LinkVault.Infrastructure.Persistence.Repositories;

public class EmailConfirmationTokenRepository(AppDbContext dbContext)
    : BaseRepository<EmailConfirmationTokenEntity>(dbContext), IEmailConfirmationTokenRepository
{
    public async Task<EmailConfirmationTokenEntity?> FindByTokenAsync(
        string token,
        ConfirmationTokenType type,
        CancellationToken ct = default)
        => await Context.EmailConfirmationTokens
            .FirstOrDefaultAsync(e => e.Token == token && e.Type == type && e.IsUsed == false, ct);

    public async Task InvalidateExistingAsync(
        Guid userId,
        ConfirmationTokenType type,
        CancellationToken ct = default)
        => await Context.EmailConfirmationTokens
            .Where(t => t.UserId == userId
                && t.Type == type
                && t.IsUsed == false)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsUsed, true), ct);
}