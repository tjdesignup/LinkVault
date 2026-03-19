using LinkVault.Application.Abstractions;
using MassTransit;

namespace LinkVault.Infrastructure.Messaging;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        await _publishEndpoint.Publish(@event, cancellationToken);
    }
}