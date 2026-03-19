using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Infrastructure.AI;
using LinkVault.Infrastructure.BackgroundJobs;
using LinkVault.Infrastructure.Cache;
using LinkVault.Infrastructure.Email;
using LinkVault.Infrastructure.Encryption;
using LinkVault.Infrastructure.Identity;
using LinkVault.Infrastructure.Messaging;
using LinkVault.Infrastructure.Payments;
using LinkVault.Infrastructure.Persistence;
using LinkVault.Infrastructure.Persistence.Queries;
using LinkVault.Infrastructure.Persistence.Repositories;
using LinkVault.Infrastructure.Storage;
using LinkVault.Domain.Abstractions.IRepositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Resend;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Memory;

namespace LinkVault.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        InfrastructureOptions options)
    {
        var keyProvider = new VaultKeyProvider(
                Environment.GetEnvironmentVariable("INFISICAL_CLIENT_ID") ?? null!,
                Environment.GetEnvironmentVariable("INFISICAL_CLIENT_SECRET") ?? null!,
                Environment.GetEnvironmentVariable("INFISICAL_PROJECT_ID") ?? null!
        );

        services.AddSingleton<IVaultKeyProvider>(keyProvider);

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
            RabbitMqPassword: password

        );


        services
            .AddDatabase(options.ConnectionString)
            .AddRepositories()
            .AddQueries()
            .AddEncryption()
            .AddAuth(options.JwtIssuer, options.JwtAudience)
            .AddRedis(options.RedisConnectionString)
            .AddEmail(options.ResendApiKey, options.EmailFromAddress)
            .AddStripe(options.StripeSecretKey, options.StripePriceId, options.StripeSuccessUrl, options.StripeCancelUrl, options.StripeWebhookSecret)
            .AddAi(options.AnthropicApiKey)
            .AddStorage(options.QuarantinePath, options.ClamAvHost, options.ClamAvPort)
            .AddMessaging(options.RabbitMqHost, options.RabbitMqUsername, options.RabbitMqPassword)
            .AddBackgroundJobs();

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(connectionString));
        return services;
    }

    private static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<ICurrentSubscriptionRepository, CurrentSubscriptionRepository>();

        return services;
    }

    private static IServiceCollection AddQueries(
        this IServiceCollection services)
    {
        services.AddScoped<ILinkQueries, LinkQueries>();
        services.AddScoped<ICollectionQueries, CollectionQueries>();
        services.AddScoped<IDeviceQueries, DeviceQueries>();
        services.AddScoped<ISubscriptionQueries, SubscriptionQueries>();
        services.AddScoped<IFileQueries, FileQueries>();

        return services;
    }

        private static IServiceCollection AddEncryption(
        this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddScoped<IEncryptionService>(sp =>
            new EncryptionService(
                sp.GetRequiredService<IVaultKeyProvider>(),
                 sp.GetRequiredService<IMemoryCache>())
        );

        return services;
    }

    private static IServiceCollection AddAuth(
        this IServiceCollection services,
        string jwtIssuer,
        string jwtAudience)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUser>(sp =>
            new CurrentUserService(
                sp.GetRequiredService<IHttpContextAccessor>()));

        services.AddSingleton<ITokenService>(sp =>
            new TokenService(
                sp.GetRequiredService<VaultKeyProvider>(),jwtIssuer,jwtAudience));

        return services;
    }

    private static IServiceCollection AddRedis(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(connectionString));

        services.AddScoped<IRedisLockService, RedisLockService>();
        services.AddScoped<IBruteForceProtectionService, BruteForceProtectionService>();

        return services;
    }

    private static IServiceCollection AddEmail(
        this IServiceCollection services,
        string apiKey,
        string fromAddress)
    {
        services.AddOptions<ResendClientOptions>()
            .Configure(options => options.ApiToken = apiKey);
            
        services.AddHttpClient<IResend, ResendClient>();

        services.AddScoped<IEmailService>(sp =>
            new ResendEmailService(
                sp.GetRequiredService<IResend>(),
                fromAddress));

        return services;
    }

    private static IServiceCollection AddStripe(
        this IServiceCollection services,
        string secretKey,
        string priceId,
        string successUrl,
        string cancelUrl,
        string webhookSecret)
    {
        services.AddSingleton<IStripeService>(_ =>
            new StripeService(secretKey, priceId, successUrl, cancelUrl, webhookSecret));

        return services;
    }

    private static IServiceCollection AddAi(
        this IServiceCollection services,
        string apiKey)
    {
        services.AddSingleton<IAiSummaryService>(_ =>
            new AnthropicAiService(apiKey));

        return services;
    }

    private static IServiceCollection AddStorage(
        this IServiceCollection services,
        string quarantinePath,
        string clamAvHost,
        int clamAvPort)
    {
        services.AddSingleton<IQuarantineStorageService>(_ =>
            new QuarantineStorageService(quarantinePath));

        services.AddScoped<IVirusScanService>(sp =>
            new ClamAvVirusScanService(
                clamAvHost,
                clamAvPort,
                sp.GetRequiredService<IQuarantineStorageService>()));

        return services;
    }

    private static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services)
    {
        services.AddHostedService<HardDeleteJob>();

        return services;
    }
}