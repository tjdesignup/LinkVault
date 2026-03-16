using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Account.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Account;

public class RequestEmailChangeHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly IEmailService _emailService;
    private readonly ICurrentUser _currentUser;
    private readonly RequestEmailChangeHandler _handler;

    public RequestEmailChangeHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _emailService = Substitute.For<IEmailService>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new RequestEmailChangeHandler(
            _userRepository,
            _tokenRepository,
            _unitOfWork,
            _encryptionService,
            _emailService,
            _currentUser);

        _currentUser.UserId.Returns(Guid.NewGuid());
        _encryptionService.ComputeBlindIndexHash(Arg.Any<string>()).Returns("new-blind-hash");
        _encryptionService.DecryptDek(Arg.Any<string>()).Returns("plaintext-dek");
        _encryptionService.Encrypt(Arg.Any<string>(), Arg.Any<string>()).Returns("encrypted-value");
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfirmedUser());
        _userRepository
            .FindByEmailBlindIndexHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull(); 
    }

    private static RequestEmailChangeCommand ValidCommand() => new(
        NewEmail: "new@example.com",
        CurrentPassword: "Password123");

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnSuccessMessage()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Message.Should().NotBeNull();
        result.Message.Should().Contain("change");
        result.Message.Should().Contain("successfully");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSetPendingEmailOnUser()
    {
        UserEntity? capturedUser = null;
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedUser = CreateConfirmedUser();
                return capturedUser;
            });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedUser!.PendingEmailEncrypted.Should().NotBeNullOrEmpty();
        capturedUser.PendingEmailBlindIndexHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldCreateEmailChangeToken()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _tokenRepository.Received(1).AddAsync(
            Arg.Is<EmailConfirmationTokenEntity>(t =>
                t.Type == ConfirmationTokenType.EmailChange &&
                !string.IsNullOrEmpty(t.Token)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldInvalidateExistingTokens()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _tokenRepository.Received(1).InvalidateExistingAsync(
            Arg.Any<Guid>(),
            ConfirmationTokenType.EmailChange,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSendConfirmationEmailToNewAddress()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _emailService.Received(1).SendEmailChangeConfirmationAsync(
            Arg.Is<string>(email => email == "new@example.com"),
            Arg.Is<string>(link => link.Contains("confirm-email-change?token=")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveBeforeSendingEmail()
    {
        var saveOrder = 0;
        var emailOrder = 0;
        var counter = 0;

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { saveOrder = ++counter; return 1; });

        _emailService.SendEmailChangeConfirmationAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => { emailOrder = ++counter; return Task.CompletedTask; });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        saveOrder.Should().BeLessThan(emailOrder);
    }

    [Fact]
    public async Task Handle_WhenWrongPassword_ShouldThrowInvalidPasswordException()
    {
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidPasswordException>();
    }

    [Fact]
    public async Task Handle_WhenNewEmailSameAsCurrent_ShouldThrowArgumentException()
    {
        var user = CreateConfirmedUser();

        _encryptionService.ComputeBlindIndexHash(Arg.Any<string>())
            .Returns(user.EmailBlindIndexHash); 

        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<EmailAlreadyConfirmedException>();
    }

    [Fact]
    public async Task Handle_WhenNewEmailAlreadyInUse_ShouldThrowEmailAlreadyInUseException()
    {
        _userRepository
            .FindByEmailBlindIndexHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfirmedUser());

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<EmailAlreadyInUseException>();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowResourceNotFoundException()
    {
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    private static UserEntity CreateConfirmedUser()
    {
        var user = UserEntity.Register(
            emailEncrypted: "encrypted",
            emailBlindIndex: "current-blind-hash",
            firstNameEncrypted: "encrypted",
            surNameEncrypted: "encrypted",
            passwordHash: "hashed-password",
            encryptedDek: "dek");
        user.ConfirmEmail();
        return user;
    }
}