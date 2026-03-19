namespace LinkVault.Application.Abstractions;

public interface IVaultKeyProvider
{
    Task<string> GetMasterKeyAsync(CancellationToken cancellationToken = default);
    Task<string> GetBlindIndexSecretAsync(CancellationToken cancellationToken = default);
    Task<string> GetJwtSecretAsync(CancellationToken cancellationToken = default);
    Task<string> GetRedisConnectionStringAsync(CancellationToken ct = default);
    Task<string> GetResendApiKeyAsync(CancellationToken cancellationToken = default);
    Task<string> GetResendEmailAsync(CancellationToken cancellationToken = default);
    Task<string> GetStripeKeyAsync(CancellationToken ct = default);
    Task<string> GetStripePriceIdAsync(CancellationToken ct = default);
    Task<string> GetStripeWebhookSecretAsync(CancellationToken ct = default);
    Task<string> GetDatabaseConnectionStringAsync(CancellationToken ct = default);
    Task<string> GetAiApiKeyAsync(CancellationToken ct = default);
    Task<(string Username, string Password)> GetRabbitMqCredentialsAsync(CancellationToken ct = default);
}