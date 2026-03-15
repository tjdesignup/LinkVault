using MediatR;

namespace LinkVault.Application.Subscriptions.Commands;

public record CreateCheckoutSessionCommand : IRequest<string>;