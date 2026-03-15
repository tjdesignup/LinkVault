using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Auth.Commands;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Abstractions;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Auth;

public class ConfirmEmailHandlerTests
{
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IEncryptionService _encryptionService;
    private readonly ConfirmEmailHandler _handler;

    public ConfirmEmailHandlerTests()
    {
        _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _emailService = Substitute.For<IEmailService>();
        _encryptionService = Substitute.For<IEncryptionService>();

        _handler = new ConfirmEmailHandler(
            _tokenRepository,
            _userRepository,
            _encryptionService,
            _unitOfWork,
            _emailService);

        _tokenRepository
            .FindByTokenAsync(Arg.Any<string>(), ConfirmationTokenType.Registration, Arg.Any<CancellationToken>())
            .Returns(CreateValidToken());

        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateUnconfirmedUser());
    }

    private static ConfirmEmailCommand ValidCommand() => new("valid-token-123");

    [Fact]
    public async Task Handle_WhenValidToken_ShouldReturnSuccessMessage()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldConfirmUserEmail()
    {
        UserEntity? capturedUser = null;
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedUser = CreateUnconfirmedUser();
                return capturedUser;
            });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedUser!.EmailConfirmed.Should().BeTrue();
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
    public async Task Handle_WhenTokenExpired_ShouldCreateNewEmailToken()
    {
        _tokenRepository
            .FindByTokenAsync(Arg.Any<string>(), Arg.Any<ConfirmationTokenType>(), Arg.Any<CancellationToken>())
            .Returns(CreateExpiredToken());

        await Assert.ThrowsAsync<ConfirmationTokenExpiredException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _tokenRepository.Received(1).AddAsync(
            Arg.Is<EmailConfirmationTokenEntity>(t =>
                t.Type == ConfirmationTokenType.Registration &&
                !string.IsNullOrEmpty(t.Token) &&
                t.IsUsed == false),
            Arg.Any<CancellationToken>());

    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ShouldDeleteExpiredToken()
    {
        _tokenRepository
            .FindByTokenAsync(Arg.Any<string>(), Arg.Any<ConfirmationTokenType>(), Arg.Any<CancellationToken>())
            .Returns(CreateExpiredToken());

        await Assert.ThrowsAsync<ConfirmationTokenExpiredException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _tokenRepository.Received(1).DeleteAsync(
            Arg.Is<EmailConfirmationTokenEntity>(t => t.Type == ConfirmationTokenType.Registration),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ShouldSendConfirmationEmail()
    {
        _tokenRepository
            .FindByTokenAsync(Arg.Any<string>(), Arg.Any<ConfirmationTokenType>(), Arg.Any<CancellationToken>())
            .Returns(CreateExpiredToken());

        await Assert.ThrowsAsync<ConfirmationTokenExpiredException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _emailService.Received(1).SendRegistrationConfirmationAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

        [Fact]
    public async Task Handle_WhenTokenExpired_ShouldSaveChanges()
    {
        _tokenRepository
            .FindByTokenAsync(Arg.Any<string>(), Arg.Any<ConfirmationTokenType>(), Arg.Any<CancellationToken>())
            .Returns(CreateExpiredToken());

        await Assert.ThrowsAsync<ConfirmationTokenExpiredException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowInvalidOperationException()
    {
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRegisterException>();
    }

    private static EmailConfirmationTokenEntity CreateValidToken()
        => EmailConfirmationTokenEntity.Create(
            Guid.NewGuid(),
            "valid-token-123",
            ConfirmationTokenType.Registration);

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

    private static UserEntity CreateUnconfirmedUser() => UserEntity.Register(
        emailEncrypted: "encrypted",
        emailBlindIndex: "blind-hash",
        firstNameEncrypted: "encrypted",
        surNameEncrypted: "encrypted",
        passwordHash: "hashed-password",
        encryptedDek: "dek");
}