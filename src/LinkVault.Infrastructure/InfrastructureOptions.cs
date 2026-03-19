namespace LinkVault.Infrastructure;

public sealed record InfrastructureOptions(
    // Database
    string ConnectionString,

    string MasterKey,
    string BlindIndexSecret,

    // JWT
    string JwtSecret,
    string JwtIssuer,
    string JwtAudience,

    // Redis
    string RedisConnectionString,

    // Email
    string ResendApiKey,
    string EmailFromAddress,

    // Stripe
    string StripeSecretKey,
    string StripePriceId,
    string StripeSuccessUrl,
    string StripeCancelUrl,
    string StripeWebhookSecret,

    // Anthropic
    string AnthropicApiKey,

    // Storage
    string QuarantinePath,
    string ClamAvHost,
    int ClamAvPort,

    // RabbitMQ
    string RabbitMqHost,
    string RabbitMqUsername,
    string RabbitMqPassword,

    string R2AccountId,
    string R2AccessKeyId,
    string R2SecretAccessKey,
    string R2BucketName
);