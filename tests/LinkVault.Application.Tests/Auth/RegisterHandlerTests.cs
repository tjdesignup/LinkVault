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

public class RegisterHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly ICurrentSubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly IEmailService _emailService;
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
        _subscriptionRepository = Substitute.For<ICurrentSubscriptionRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _emailService = Substitute.For<IEmailService>();

        _handler = new RegisterHandler(
            _userRepository,
            _tokenRepository,
            _subscriptionRepository,
            _unitOfWork,
            _encryptionService,
            _emailService);

        _encryptionService.GenerateEncryptedDek().Returns("encrypted-dek");
        _encryptionService.DecryptDek("encrypted-dek").Returns("plaintext-dek");
        _encryptionService.ComputeBlindIndexHash(Arg.Any<string>()).Returns("blind-hash");
        _encryptionService.Encrypt(Arg.Any<string>(), Arg.Any<string>()).Returns("encrypted-value");
        _encryptionService.HashPassword(Arg.Any<string>()).Returns("hashed-password");
        _userRepository.FindByEmailBlindIndexHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();
    }

    private static RegisterCommand ValidCommand() => new(
        Email: "tomas@example.com",
        Password: "Password123",
        FirstName: "Tomáš",
        Surname: "Novák");

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnConfirmationMessage()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Message.Should().NotBeNull();
        result.Message.Should().ContainEquivalentOf("Registration");
        result.Message.Should().ContainEquivalentOf("successful");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldAddUserToRepository()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _userRepository.Received(1).AddAsync(
            Arg.Is<UserEntity>(u =>
                u.EmailBlindIndexHash == "blind-hash" &&
                u.EmailEncrypted == "encrypted-value"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldCreateFreeSubscription()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _subscriptionRepository.Received(1).AddAsync(
            Arg.Is<CurrentSubscriptionEntity>(s => s.Tier == SubscriptionTier.Free),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldCreateConfirmationToken()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _tokenRepository.Received(1).AddAsync(
            Arg.Is<EmailConfirmationTokenEntity>(t => 
                t.Type == ConfirmationTokenType.Registration &&
                !string.IsNullOrEmpty(t.Token) &&
                t.IsUsed == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSendConfirmationEmail()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _emailService.Received(1).SendRegistrationConfirmationAsync(
            Arg.Is<string>(email => email == "tomas@example.com"),
            Arg.Is<string>(link => link.Contains("confirm-email?token=")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChangesOnce()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldEncryptThreeFields()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _encryptionService.Received(3).Encrypt(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveBeforeSendingEmail()
    {
        var saveOrder = 0;
        var emailOrder = 0;
        var counter = 0;

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { saveOrder = ++counter; return 1; });

        _emailService.SendRegistrationConfirmationAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => { emailOrder = ++counter; return Task.CompletedTask; });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        saveOrder.Should().BeLessThan(emailOrder);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldThrowEmailAlreadyInUseException()
    {
        _userRepository.FindByEmailBlindIndexHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateExistingUser());

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<EmailAlreadyInUseException>();
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldNotSaveChanges()
    {
        _userRepository.FindByEmailBlindIndexHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateExistingUser());

        await Assert.ThrowsAsync<EmailAlreadyInUseException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldNotSendEmail()
    {
        _userRepository.FindByEmailBlindIndexHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateExistingUser());

        await Assert.ThrowsAsync<EmailAlreadyInUseException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _emailService.DidNotReceive().SendRegistrationConfirmationAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static UserEntity CreateExistingUser() => UserEntity.Register(
        emailEncrypted: "encrypted",
        emailBlindIndex: "blind-hash",
        firstNameEncrypted: "encrypted",
        surNameEncrypted: "encrypted",
        passwordHash: "hash",
        encryptedDek: "dek");
}