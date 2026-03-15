using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using LinkVault.Application.Links.Commands;
using LinkVault.Application.Links.Events;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;
using NSubstitute;

namespace LinkVault.Application.Tests.Links;

public class AddLinkHandlerTests
{
    private readonly ILinkRepository _linkRepository;
    private readonly ILinkQueries _linkQueries;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentSubscriptionRepository _subscriptionRepository;
    private readonly IRedisLockService _redisLock;
    private readonly IEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AddLinkHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public AddLinkHandlerTests()
    {
        _linkRepository = Substitute.For<ILinkRepository>();
        _linkQueries = Substitute.For<ILinkQueries>();
        _currentUser = Substitute.For<ICurrentUser>();
        _subscriptionRepository = Substitute.For<ICurrentSubscriptionRepository>();
        _redisLock = Substitute.For<IRedisLockService>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new AddLinkHandler(
            _linkRepository,
            _linkQueries,
            _currentUser,
            _subscriptionRepository,
            _redisLock,
            _eventPublisher,
            _unitOfWork);

        _currentUser.UserId.Returns(_userId);
        _currentUser.IsProTier.Returns(false);

        _redisLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _redisLock.IsLockedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _linkRepository.CountByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(0);
        _subscriptionRepository.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(CreateFreeSubscription());
        _linkQueries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateLinkDto());
    }

    private static AddLinkCommand ValidCommand() => new(
        Url: "https://github.com",
        Title: "GitHub",
        Note: null,
        Tags: ["dotnet"]);

    // -------------------------------------------------------
    // Blok A — úspěšné přidání záložky
    // -------------------------------------------------------

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnLinkDto()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<LinkDto>();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldAddLinkToRepository()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _linkRepository.Received(1).AddAsync(
            Arg.Is<LinkEntity>(l =>
                l.UserId == _userId &&
                l.Url.Value == "https://github.com" &&
                l.Title == "GitHub"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldPublishLinkCreatedEvent()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            Arg.Any<LinkCreatedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldPublishEventAfterSave()
    {
        var saveOrder = 0;
        var publishOrder = 0;
        var counter = 0;

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { saveOrder = ++counter; return 1; });

        _eventPublisher.PublishAsync(Arg.Any<LinkCreatedEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ => { publishOrder = ++counter; return Task.CompletedTask; });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        saveOrder.Should().BeLessThan(publishOrder);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------
    // Blok B — lock lifecycle
    // -------------------------------------------------------

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldAcquireLockWithCorrectKey()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _redisLock.Received(1).AcquireAsync(
            Arg.Is<string>(key => key == $"add-link:{_userId}"),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldAcquireLockWithReasonableTtl()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _redisLock.Received(1).AcquireAsync(
            Arg.Any<string>(),
            Arg.Is<TimeSpan>(ttl => ttl.TotalSeconds > 0 && ttl.TotalSeconds <= 30),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReleaseLockAfterSave()
    {
        var saveOrder = 0;
        var releaseOrder = 0;
        var counter = 0;

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { saveOrder = ++counter; return 1; });

        _redisLock.ReleaseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => { releaseOrder = ++counter; return Task.CompletedTask; });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        saveOrder.Should().BeLessThan(releaseOrder);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldAlwaysReleaseLock()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _redisLock.Received(1).ReleaseAsync(
            Arg.Is<string>(key => key == $"add-link:{_userId}"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ShouldStillReleaseLock()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new Exception("DB error"));

        await Assert.ThrowsAsync<Exception>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _redisLock.Received(1).ReleaseAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------
    // Blok C — lock expiroval během zpracování
    // -------------------------------------------------------

    [Fact]
    public async Task Handle_WhenLockExpiredDuringProcessing_ShouldThrowInvalidOperationException()
    {
        _redisLock.IsLockedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenLockExpiredDuringProcessing_ShouldNotSaveChanges()
    {
        _redisLock.IsLockedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLockExpiredDuringProcessing_ShouldStillReleaseLock()
    {
        _redisLock.IsLockedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _redisLock.Received(1).ReleaseAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------
    // Blok D — lock nedostupný
    // -------------------------------------------------------

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ShouldThrowInvalidOperationException()
    {
        _redisLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ShouldNotSaveChanges()
    {
        _redisLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ShouldNotPublishEvent()
    {
        _redisLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<LinkCreatedEvent>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------
    // Blok E — free tier limit
    // -------------------------------------------------------

    [Fact]
    public async Task Handle_WhenFreeTierLimitReached_ShouldThrowLinkLimitExceededException()
    {
        _linkRepository.CountByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(50);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<LinkLimitExceededException>();
    }

    [Fact]
    public async Task Handle_WhenFreeTierLimitReached_ShouldStillReleaseLock()
    {
        _linkRepository.CountByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(50);

        await Assert.ThrowsAsync<LinkLimitExceededException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _redisLock.Received(1).ReleaseAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProTierAndOver50Links_ShouldNotThrow()
    {
        _currentUser.IsProTier.Returns(true);
        _subscriptionRepository.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(CreateProSubscription());
        _linkRepository.CountByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(100);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    private static CurrentSubscriptionEntity CreateFreeSubscription()
        => CurrentSubscriptionEntity.CreateFree(Guid.NewGuid(), "stripe-customer-123");

    private static CurrentSubscriptionEntity CreateProSubscription()
    {
        var userId = Guid.NewGuid();
        var subscription = CurrentSubscriptionEntity.CreateFree(userId, "stripe-customer-123");
        subscription.ApplyEvent(SubscriptionEventEntity.Create(
            userId,  // ← stejný userId
            SubscriptionEventType.Activated,
            SubscriptionTier.Pro,
            SubscriptionStatus.Active,
            "stripe-sub-123",
            DateTime.UtcNow.AddMonths(1)));
        return subscription;
    }

    private static LinkDto CreateLinkDto() => new(
        Id: Guid.NewGuid(),
        Url: "https://github.com",
        Title: "GitHub",
        Note: null,
        Tags: ["dotnet"],
        OgTitle: null,
        OgDescription: null,
        OgImageUrl: null,
        MetadataStatus: "Pending",
        Attachments: [],
        CreatedAt: DateTime.UtcNow);
}