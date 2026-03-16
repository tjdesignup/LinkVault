using LinkVault.Application.DTOs;
using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Account.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Account;

public class ChangePasswordHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly ICurrentUser _currentUser;
    private readonly ChangePasswordHandler _handler;

    public ChangePasswordHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new ChangePasswordHandler(
            _userRepository,
            _refreshTokenRepository,
            _unitOfWork,
            _encryptionService,
            _currentUser);

        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _encryptionService.HashPassword(Arg.Any<string>()).Returns("new-hashed-password");
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfirmedUser());
    }

    private static ChangePasswordCommand ValidCommand() => new(
        CurrentPassword: "OldPassword123",
        NewPassword: "NewPassword456",
        NewPasswordConfirmation: "NewPassword456");

    private static ChangePasswordCommand InvalidCommand() => new(
        CurrentPassword: "NewPassword456",
        NewPassword: "NewPassword456",
        NewPasswordConfirmation: "NewPassword456");

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnUnit()
    {
        _encryptionService.VerifyPassword(ValidCommand().NewPassword, Arg.Any<string>()).Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Message.Should().NotBeNull();
        result.Message.Should().Contain("successfully");
        result.Message.Should().ContainEquivalentOf("password");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldChangePasswordHash()
    {
        UserEntity? capturedUser = null;
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedUser = CreateConfirmedUser();
                return capturedUser;
            });

        _encryptionService.VerifyPassword(ValidCommand().NewPassword, Arg.Any<string>()).Returns(false);
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedUser!.PasswordHash.Should().Be("new-hashed-password");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldRevokeAllTokens()
    {
        _encryptionService.VerifyPassword(ValidCommand().NewPassword, Arg.Any<string>()).Returns(false);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _refreshTokenRepository.Received(1).RevokeAllByUserIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChanges()
    {
        _encryptionService.VerifyPassword(ValidCommand().NewPassword, Arg.Any<string>()).Returns(false);
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWrongCurrentPassword_ShouldThrowInvalidPasswordException()
    {
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidPasswordException>();
    }

    [Fact]
    public async Task Handle_WhenNewPasswordSameAsOld_ShouldThrowInvalidPasswordException()
    {
        _encryptionService
            .VerifyPassword(InvalidCommand().CurrentPassword, Arg.Any<string>())
            .Returns(true);
        _encryptionService
            .VerifyPassword(InvalidCommand().NewPassword, Arg.Any<string>())
            .Returns(true);

        var act = async () => await _handler.Handle(InvalidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidPasswordException>();
    }

    [Fact]
    public async Task Handle_WhenWrongCurrentPassword_ShouldNotSaveChanges()
    {
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidPasswordException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowInvalidOperationException()
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
            emailBlindIndex: "blind-hash",
            firstNameEncrypted: "encrypted",
            surNameEncrypted: "encrypted",
            passwordHash: "old-hashed-password",
            encryptedDek: "dek");
        user.ConfirmEmail();
        return user;
    }
}