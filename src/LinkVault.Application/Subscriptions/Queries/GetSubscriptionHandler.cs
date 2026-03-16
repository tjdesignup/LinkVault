using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Subscriptions.Queries;

public class GetSubscriptionHandler(
    ISubscriptionQueries subscriptionQueries,
    ICurrentUser currentUser)
    : IRequestHandler<GetSubscriptionQuery, SubscriptionDto?>
{
    public async Task<SubscriptionDto?> Handle(
        GetSubscriptionQuery query,
        CancellationToken cancellationToken)
        => await subscriptionQueries.GetByUserIdAsync(
            currentUser.UserId,
            cancellationToken);
}