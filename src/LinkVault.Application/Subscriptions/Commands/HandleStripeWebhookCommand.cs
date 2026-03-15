using MediatR;

namespace LinkVault.Application.Subscriptions.Commands;

public record HandleStripeWebhookCommand(
    string Payload,
    string StripeSignature
) : IRequest<Unit>;