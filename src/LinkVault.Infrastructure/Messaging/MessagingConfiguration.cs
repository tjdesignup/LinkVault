// src/LinkVault.Infrastructure/Messaging/MessagingConfiguration.cs

using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace LinkVault.Infrastructure.Messaging;

public static class MessagingConfiguration
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        string host,
        string username,
        string password)
    {
        services.AddMassTransit(x =>
        {
            // Consumers budou přidány ve Fázi 5
            // x.AddConsumer<FileScannedConsumer>();
            // x.AddConsumer<LinkMetadataConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(5)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}