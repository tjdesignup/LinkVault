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

namespace LinkVault.Application.Tests.Auth;

public class LoginHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICurrentSubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly ITokenService _tokenService;
    private readonly IBruteForceProtectionService _bruteForce;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _subscriptionRepository = Substitute.For<ICurrentSubscriptionRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _tokenService = Substitute.For<ITokenService>();
        _bruteForce = Substitute.For<IBruteForceProtectionService>();

        _handler = new LoginHandler(
            _userRepository,
            _refreshTokenRepository,
            _subscriptionRepository,
            _unitOfWork,
            _encryptionService,
            _tokenService,
            _bruteForce);

        _encryptionService.ComputeBlindIndexHash(Arg.Any<string>()).Returns("blind-hash");
        _encryptionService.DecryptDek(Arg.Any<string>()).Returns("plaintext-dek");
        _encryptionService.Decrypt(Arg.Any<string>(), Arg.Any<string>()).Returns("tomas@example.com");
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<string>())
            .Returns("access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");
        _tokenService.HashRefreshToken("refresh-token").Returns("refresh-token-hash");
        _bruteForce.IsLockedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _bruteForce.RecordFailedAttemptAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.FindByEmailBlindIndexHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfirmedUser());
        _subscriptionRepository.FindByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateFreeSubscription(Guid.NewGuid()));
    }

    private static LoginCommand ValidCommand() => new(
        Email: "tomas@example.com",
        Password: "Password123",
        DeviceName: "Chrome / Windows",
        IpAddress: "127.0.0.1");

    [Fact]
    public async Task Handle_WhenValidCredentials_ShouldReturnAuthResultDto()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenValidCredentials_ShouldSaveRefreshToken()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _refreshTokenRepository.Received(1).AddAsync(
            Arg.Is<RefreshTokenEntity>(t =>
                t.TokenHash == "refresh-token-hash" &&
                t.DeviceName == "Chrome / Windows" &&
                t.IpAddress == "127.0.0.1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCredentials_ShouldResetBruteForceCounter()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _bruteForce.Received(1).ResetAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCredentials_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCredentials_ShouldIncludeCorrectTierInResult()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _tokenService.Received(1).GenerateAccessToken(
            Arg.Any<Guid>(),
            Arg.Any<bool>(),
            Arg.Any<string>()
        );
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowInvalidLoginException()
    {
        _userRepository.FindByEmailBlindIndexHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidLoginException>();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldNotSaveChanges()
    {
        _userRepository.FindByEmailBlindIndexHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        await Assert.ThrowsAsync<InvalidLoginException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailNotConfirmed_ShouldThrowEmailNotConfirmedException()
    {
        _userRepository.FindByEmailBlindIndexHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateUnconfirmedUser());

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<EmailNotConfirmedException>();
    }

    [Fact]
    public async Task Handle_WhenEmailNotConfirmed_ShouldNotVerifyPassword()
    {
        _userRepository.FindByEmailBlindIndexHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateUnconfirmedUser());

        await Assert.ThrowsAsync<EmailNotConfirmedException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        _encryptionService.DidNotReceive().VerifyPassword(
            Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenUserDeleted_ShouldThrowUserDeletedException()
    {
        _userRepository.FindByEmailBlindIndexHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateDeletedUser());

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UserDeletedException>();
    }

    [Fact]
    public async Task Handle_WhenAccountLocked_ShouldThrowAccountLockedException()
    {
        _bruteForce.IsLockedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _bruteForce.GetRemainingLockTimeAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(TimeSpan.FromMinutes(3));

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<AccountLockedException>();
    }

    [Fact]
    public async Task Handle_WhenAccountLocked_ShouldNotVerifyPassword()
    {
        _bruteForce.IsLockedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _bruteForce.GetRemainingLockTimeAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(TimeSpan.FromMinutes(3));

        await Assert.ThrowsAsync<AccountLockedException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        _encryptionService.DidNotReceive().VerifyPassword(
            Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenWrongPassword_ShouldThrowInvalidPasswordException()
    {
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidLoginException>();
    }

    [Fact]
    public async Task Handle_WhenWrongPassword_ShouldRecordFailedAttempt()
    {
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidLoginException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _bruteForce.Received(1).RecordFailedAttemptAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWrongPassword_ShouldNotSaveChanges()
    {
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidLoginException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWrongPasswordCausesLockout_ShouldRevokeAllTokens()
    {
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);
        _bruteForce.RecordFailedAttemptAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<InvalidLoginException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _refreshTokenRepository.Received(1).RevokeAllByUserIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    private static UserEntity CreateConfirmedUser(string? passwordHash = null)
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

    private static UserEntity CreateUnconfirmedUser() => UserEntity.Register(
        emailEncrypted: "encrypted",
        emailBlindIndex: "blind-hash",
        firstNameEncrypted: "encrypted",
        surNameEncrypted: "encrypted",
        passwordHash: "hashed-password",
        encryptedDek: "dek");

    private static UserEntity CreateDeletedUser()
    {
        var user = CreateConfirmedUser();
        user.Delete();
        return user;
    }

    private static CurrentSubscriptionEntity CreateFreeSubscription(Guid userId)
        => CurrentSubscriptionEntity.CreateFree(userId, "stripe-customer-123");
}