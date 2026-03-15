using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Subscriptions.Queries;

public record GetSubscriptionQuery : IRequest<SubscriptionDto?>;