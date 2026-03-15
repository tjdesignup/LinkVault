using FluentAssertions;
using LinkVault.Application.Account.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Account;

public class ConfirmEmailChangeHandlerTests
{
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ConfirmEmailChangeHandler _handler;

    public ConfirmEmailChangeHandlerTests()
    {
        _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new ConfirmEmailChangeHandler(
            _tokenRepository,
            _userRepository,
            _refreshTokenRepository,
            _unitOfWork);

        _tokenRepository
            .FindByTokenAsync(Arg.Any<string>(), ConfirmationTokenType.EmailChange, Arg.Any<CancellationToken>())
            .Returns(CreateValidToken());

        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateUserWithPendingEmail());
    }

    private static ConfirmEmailChangeCommand ValidCommand() => new("valid-token-123");

    [Fact]
    public async Task Handle_WhenValidToken_ShouldReturnSuccessMessage()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().Be("Email change confirmed successfully.");
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldPromotePendingEmailToCurrent()
    {
        UserEntity? capturedUser = null;
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedUser = CreateUserWithPendingEmail();
                return capturedUser;
            });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedUser!.PendingEmailEncrypted.Should().BeNull();
        capturedUser.PendingEmailBlindIndexHash.Should().BeNull();
        capturedUser.EmailEncrypted.Should().Be("new-encrypted-email");
        capturedUser.EmailBlindIndexHash.Should().Be("new-blind-hash");
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldRevokeAllRefreshTokens()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _refreshTokenRepository.Received(1).RevokeAllByUserIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldThrowInvalidConfirmationTokenException()
    {
        _tokenRepository
            .FindByTokenAsync(Arg.Any<string>(), Arg.Any<ConfirmationTokenType>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidConfirmationTokenException>();
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldNotSaveChanges()
    {
        _tokenRepository
            .FindByTokenAsync(Arg.Any<string>(), Arg.Any<ConfirmationTokenType>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        await Assert.ThrowsAsync<InvalidConfirmationTokenException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenAlreadyUsed_ShouldThrowInvalidConfirmationTokenException()
    {
        _tokenRepository
            .FindByTokenAsync(Arg.Any<string>(), Arg.Any<ConfirmationTokenType>(), Arg.Any<CancellationToken>())
            .Returns(CreateUsedToken());

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidConfirmationTokenException>();
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ShouldThrowInvalidConfirmationTokenException()
    {
        _tokenRepository
            .FindByTokenAsync(Arg.Any<string>(), Arg.Any<ConfirmationTokenType>(), Arg.Any<CancellationToken>())
            .Returns(CreateExpiredToken());

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConfirmationTokenExpiredException>();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowInvalidOperationException()
    {
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static EmailConfirmationTokenEntity CreateValidToken()
        => EmailConfirmationTokenEntity.Create(
            Guid.NewGuid(),
            "valid-token-123",
            ConfirmationTokenType.EmailChange);

    private static EmailConfirmationTokenEntity CreateUsedToken()
    {
        var token = CreateValidToken();
        token.Use();
        return token;
    }

    private static EmailConfirmationTokenEntity CreateExpiredToken()
    {
        var token = CreateValidToken();
        typeof(EmailConfirmationTokenEntity)
            .GetProperty(nameof(EmailConfirmationTokenEntity.ExpiresAt))!
            .SetValue(token, DateTime.UtcNow.AddHours(-1));
        return token;
    }

    private static UserEntity CreateUserWithPendingEmail()
    {
        var user = UserEntity.Register(
            emailEncrypted: "old-encrypted-email",
            emailBlindIndex: "old-blind-hash",
            firstNameEncrypted: "encrypted",
            surNameEncrypted: "encrypted",
            passwordHash: "hashed-password",
            encryptedDek: "dek");
        user.ConfirmEmail();
        user.RequestEmailChange("new-encrypted-email", "new-blind-hash");
        return user;
    }
}