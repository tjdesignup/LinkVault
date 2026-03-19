using Infisical.Sdk;
using Infisical.Sdk.Model;
using LinkVault.Application.Abstractions;

namespace LinkVault.Infrastructure.Encryption;

public sealed class VaultKeyProvider : IVaultKeyProvider
{
    private readonly InfisicalClient _client;
    private readonly string _projectId;
    private readonly string _environment;

    public VaultKeyProvider(string clientId, string clientSecret, string projectId, string environment = "dev")
    {
        _projectId = projectId;
        _environment = environment;

        var settings = new InfisicalSdkSettingsBuilder().Build();
        _client = new InfisicalClient(settings);
        _ = _client.Auth().UniversalAuth().LoginAsync(clientId, clientSecret).GetAwaiter().GetResult();
    }

    public async Task<string> GetMasterKeyAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("MASTER_KEY");

    public async Task<string> GetBlindIndexSecretAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("BLIND_INDEX_SECRET");

    public async Task<string> GetJwtSecretAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("JWT_SECRET");

    public async Task<string> GetRedisConnectionStringAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("REDIS_CONNECTION_STRING");

    public async Task<string> GetResendApiKeyAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("RESEND_API_KEY");

    public async Task<string> GetResendEmailAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("EMAIL");

    public async Task<string> GetStripeKeyAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("STRIPE_SECRET_KEY");

    public async Task<string> GetStripePriceIdAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("STRIPE_PRO_PRICE_ID");

    public async Task<string> GetStripeWebhookSecretAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("STRIPE_WEBHOOK_SECRET");

    public async Task<string> GetDatabaseConnectionStringAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("DB_CONNECTION_STRING");

    public async Task<string> GetAiApiKeyAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("AI_API_KEY");

    public async Task<(string Username, string Password)> GetRabbitMqCredentialsAsync(CancellationToken ct = default)
    {
        var user = await GetSecretByNameAsync("RABBITMQ_USERNAME");
        var pass = await GetSecretByNameAsync("RABBITMQ_PASSWORD");
        return (user, pass);
    }

    public async Task<string> GetR2AccountIdAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("R2_ACCOUNT_ID");
    public async Task<string> GetR2AccessKeyIdAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("R2_ACCESS_KEY_ID");
    public async Task<string> GetR2SecretAccessKeyAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("R2_SECRET_ACCESS_KEY");
    public async Task<string> GetR2BucketNameAsync(CancellationToken ct = default)
        => await GetSecretByNameAsync("R2_BUCKET_NAME");

    private async Task<string> GetSecretByNameAsync(string name)
    {
        var options = new GetSecretOptions
        {
            SecretName = name,
            EnvironmentSlug = _environment,
            ProjectId = _projectId,
            SecretPath = "/"
        };

        var secret = await _client.Secrets().GetAsync(options);

        if (secret == null || string.IsNullOrEmpty(secret.SecretValue))
        {
            throw new InvalidOperationException($"Secret '{name}' was not find in Infisical secrets (Project: {_projectId}, Env: {_environment}).");
        }

        return secret.SecretValue;
    }
}