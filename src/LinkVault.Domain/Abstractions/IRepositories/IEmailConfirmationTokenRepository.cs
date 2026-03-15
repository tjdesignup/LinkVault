using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;

namespace LinkVault.Domain.Abstractions.IRepositories;

public interface IEmailConfirmationTokenRepository : IRepository<EmailConfirmationTokenEntity>
{
    Task<EmailConfirmationTokenEntity?> FindByTokenAsync(
        string token,
        ConfirmationTokenType type,
        CancellationToken cancellationToken = default);

    Task InvalidateExistingAsync(
        Guid userId,
        ConfirmationTokenType type,
        CancellationToken cancellationToken = default);
}