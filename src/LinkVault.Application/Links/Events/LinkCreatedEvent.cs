namespace LinkVault.Application.Links.Events;

public record LinkCreatedEvent(
    Guid LinkId,
    string Url
);