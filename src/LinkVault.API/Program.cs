using LinkVault.Application;
using LinkVault.Application.Abstractions;
using LinkVault.Infrastructure;
using LinkVault.Infrastructure.Encryption;
using LinkVault.Api;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var keyProvider = new VaultKeyProvider(
    Environment.GetEnvironmentVariable("INFISICAL_CLIENT_ID")!,
    Environment.GetEnvironmentVariable("INFISICAL_CLIENT_SECRET")!,
    Environment.GetEnvironmentVariable("INFISICAL_PROJECT_ID")!
);

builder.Services.AddSingleton<IVaultKeyProvider>(keyProvider);

var (user, password) = keyProvider.GetRabbitMqCredentialsAsync().GetAwaiter().GetResult();

var infraOptions = new InfrastructureOptions(
    ConnectionString: keyProvider.GetDatabaseConnectionStringAsync().GetAwaiter().GetResult(),

    MasterKey: keyProvider.GetMasterKeyAsync().GetAwaiter().GetResult(),
    BlindIndexSecret: keyProvider.GetBlindIndexSecretAsync().GetAwaiter().GetResult(),

    JwtSecret: keyProvider.GetJwtSecretAsync().GetAwaiter().GetResult(),
    JwtIssuer: Environment.GetEnvironmentVariable("JWT_ISSUER") ?? null!,
    JwtAudience: Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? null!,

    RedisConnectionString: keyProvider.GetRedisConnectionStringAsync().GetAwaiter().GetResult(),

    ResendApiKey: keyProvider.GetResendApiKeyAsync().GetAwaiter().GetResult(),
    EmailFromAddress:keyProvider.GetResendEmailAsync().GetAwaiter().GetResult(),

    StripeSecretKey: keyProvider.GetStripeKeyAsync().GetAwaiter().GetResult(),
    StripePriceId: keyProvider.GetStripePriceIdAsync().GetAwaiter().GetResult(),
    StripeSuccessUrl: Environment.GetEnvironmentVariable("STRIPE_SUCCESS_URL") ?? null!,
    StripeCancelUrl: Environment.GetEnvironmentVariable("STRIPE_CANCEL_URL") ?? null!,
    StripeWebhookSecret: keyProvider.GetStripeWebhookSecretAsync().GetAwaiter().GetResult(),

    AnthropicApiKey: keyProvider.GetAiApiKeyAsync().GetAwaiter().GetResult(),

    QuarantinePath: Environment.GetEnvironmentVariable("QUARANTINE_PATH") ?? null!,
    ClamAvHost: Environment.GetEnvironmentVariable("CLAM_AV_HOST") ?? null!, 
    ClamAvPort: int.Parse(Environment.GetEnvironmentVariable("CLAM_AV_PORT")?? null!), 

    RabbitMqHost: Environment.GetEnvironmentVariable("RABBIT_MQ_HOST") ?? null!, 
    RabbitMqUsername: user,
    RabbitMqPassword: password,

    R2AccountId: keyProvider.GetR2AccountIdAsync().GetAwaiter().GetResult(),
    R2AccessKeyId: keyProvider.GetR2AccessKeyIdAsync().GetAwaiter().GetResult(),
    R2SecretAccessKey: keyProvider.GetR2SecretAccessKeyAsync().GetAwaiter().GetResult(),
    R2BucketName: keyProvider.GetR2BucketNameAsync().GetAwaiter().GetResult()

);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(infraOptions);
builder.Services.AddApi(keyProvider, infraOptions.JwtIssuer, infraOptions.JwtAudience);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();