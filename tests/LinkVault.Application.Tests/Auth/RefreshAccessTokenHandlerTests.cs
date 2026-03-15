using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Auth.Commands;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using LinkVault.Domain.Enums;

namespace LinkVault.Application.Tests.Auth;

public class RefreshAccessTokenHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentSubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly RefreshAccessTokenHandler _handler;

    public RefreshAccessTokenHandlerTests()
    {
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _subscriptionRepository = Substitute.For<ICurrentSubscriptionRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _tokenService = Substitute.For<ITokenService>();

        _handler = new RefreshAccessTokenHandler(
            _refreshTokenRepository,
            _userRepository,
            _subscriptionRepository,
            _unitOfWork,
            _tokenService);

        _tokenService.HashRefreshToken(Arg.Any<string>()).Returns("hashed-token");
        _tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<string>())
            .Returns("new-access-token");
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateValidToken());
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfirmedUser());
        _subscriptionRepository
            .FindByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateFreeSubscription(Guid.NewGuid()));
    }

    private static RefreshAccessTokenCommand ValidCommand() => new("plain-refresh-token","ip","device");

    [Fact]
    public async Task Handle_WhenValidToken_ShouldReturnNewAccessToken()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().Be("new-access-token");
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldNotSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldNotGenerateNewRefreshToken()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _tokenService.DidNotReceive().GenerateRefreshToken();
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldHashIncomingToken()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _tokenService.Received(1).HashRefreshToken("plain-refresh-token");
    }

    [Fact]
    public async Task Handle_WhenProTier_ShouldGenerateAccessTokenWithProTier()
    {
        _subscriptionRepository
            .FindByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateProSubscription(Guid.NewGuid()));

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _tokenService.Received(1).GenerateAccessToken(
            Arg.Any<Guid>(),
            Arg.Any<bool>(),
            Arg.Any<string>()
        );
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldThrowInvalidTokenException()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTokenException>();
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldNotSaveChanges()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        await Assert.ThrowsAsync<InvalidTokenException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenRevoked_ShouldThrowInvalidTokenException()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateRevokedToken());

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTokenException>();
    }

    [Fact]
    public async Task Handle_WhenTokenRevoked_ShouldNotSaveChanges()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateRevokedToken());

        await Assert.ThrowsAsync<InvalidTokenException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ShouldThrowInvalidTokenException()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateExpiredToken());

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTokenException>();
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ShouldRevokeToken()
    {
        var expiredToken = CreateExpiredToken();
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expiredToken);

        await Assert.ThrowsAsync<InvalidTokenException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        expiredToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ShouldSaveChanges()
    {
        _refreshTokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateExpiredToken());

        await Assert.ThrowsAsync<InvalidTokenException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowInvalidLoginException()
    {
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidLoginException>();
    }

    [Fact]
    public async Task Handle_WhenUserDeleted_ShouldThrowUserDeletedException()
    {
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateDeletedUser());

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UserDeletedException>();
    }

    private static RefreshTokenEntity CreateValidToken()
        => RefreshTokenEntity.Create(
            Guid.NewGuid(),
            "hashed-token",
            "Chrome / Windows",
            "127.0.0.1");

    private static RefreshTokenEntity CreateRevokedToken()
    {
        var token = CreateValidToken();
        token.Revoke();
        return token;
    }

    private static RefreshTokenEntity CreateExpiredToken()
    {
        var token = CreateValidToken();
        typeof(RefreshTokenEntity)
            .GetProperty(nameof(RefreshTokenEntity.ExpiresAt))!
            .SetValue(token, DateTime.UtcNow.AddDays(-1));
        return token;
    }

    private static UserEntity CreateConfirmedUser()
    {
        var user = UserEntity.Register(
            emailEncrypted: "encrypted",
            emailBlindIndex: "blind-hash",
            firstNameEncrypted: "encrypted",
            surNameEncrypted: "encrypted",
            passwordHash: "hashed-password",
            encryptedDek: "dek");
        user.ConfirmEmail();
        return user;
    }

    private static UserEntity CreateDeletedUser()
    {
        var user = CreateConfirmedUser();
        user.Delete();
        return user;
    }

    private static CurrentSubscriptionEntity CreateFreeSubscription(Guid userId)
        => CurrentSubscriptionEntity.CreateFree(userId, "stripe-customer-123");

    private static CurrentSubscriptionEntity CreateProSubscription(Guid userId)
    {
        var subscription = CurrentSubscriptionEntity.CreateFree(userId, "stripe-customer-123");
        subscription.ApplyEvent(SubscriptionEventEntity.Create(
            userId,
            SubscriptionEventType.Activated,
            SubscriptionTier.Pro,
            SubscriptionStatus.Active,
            "stripe-sub-123",
            DateTime.UtcNow.AddMonths(1)));
        return subscription;
    }
}