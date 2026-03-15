using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using LinkVault.Application.Links.Events;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;
using LinkVault.Domain.ValueObjects;
using MediatR;

namespace LinkVault.Application.Links.Commands;

public class AddLinkHandler(
    ILinkRepository linkRepository,
    ILinkQueries linkQueries,
    ICurrentUser currentUser,
    ICurrentSubscriptionRepository subscriptionRepository,
    IRedisLockService redisLock,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddLinkCommand, LinkDto>
{
    private const int FreeTierLimit = 50;
    private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(10);

    public async Task<LinkDto> Handle(
        AddLinkCommand command,
        CancellationToken cancellationToken)
    {
        var lockKey = $"add-link:{currentUser.UserId}";

        var acquired = await redisLock.AcquireAsync(lockKey, LockTtl, cancellationToken);

        if (!acquired)
            throw new InvalidOperationException("Systém je zaneprázdněn. Zkuste to znovu.");

        try
        {
            var isStillLocked = await redisLock.IsLockedAsync(lockKey, cancellationToken);
            if (!isStillLocked)
                throw new InvalidOperationException("Lock expiroval během zpracování. Zkuste to znovu.");

            var subscription = await subscriptionRepository.FindByUserIdAsync(
                currentUser.UserId, cancellationToken);

            var isProTier = subscription?.IsProActive ?? false;

            if (!isProTier)
            {
                var count = await linkRepository.CountByUserIdAsync(
                    currentUser.UserId, cancellationToken);

                if (count >= FreeTierLimit)
                    throw new LinkLimitExceededException(FreeTierLimit);
            }

            var tags = command.Tags.Select(t => new Tag(t)).ToList();
            var link = LinkEntity.Create(
                currentUser.UserId,
                new Url(command.Url),
                command.Title,
                command.Note,
                tags);

            await linkRepository.AddAsync(link, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await eventPublisher.PublishAsync(
                new LinkCreatedEvent(link.Id, command.Url),
                cancellationToken);

            return await linkQueries.GetByIdAsync(link.Id, currentUser.UserId, cancellationToken)
                ?? throw new InvalidOperationException("Link not found after creation.");
        }
        finally
        {
            await redisLock.ReleaseAsync(lockKey, cancellationToken);
        }
    }
}