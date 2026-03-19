using MediatR;

namespace LinkVault.Application.Subscriptions.Commands;

public record CancelSubscriptionCommand : IRequest<Unit>;