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

public class DeleteAccountHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly ICurrentUser _currentUser;
    private readonly DeleteAccountHandler _handler;

    public DeleteAccountHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new DeleteAccountHandler(
            _userRepository,
            _refreshTokenRepository,
            _unitOfWork,
            _encryptionService,
            _currentUser);

        _currentUser.UserId.Returns(Guid.NewGuid());
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfirmedUser());
    }

    private static DeleteAccountCommand ValidCommand() => new("Password123");

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnSuccessMessage()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().Be("Account deleted successfully.");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSoftDeleteUser()
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

        capturedUser!.IsDeleted.Should().BeTrue();
        capturedUser.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldRevokeAllRefreshTokens()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _refreshTokenRepository.Received(1).RevokeAllByUserIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
    public async Task Handle_WhenWrongPassword_ShouldNotSaveChanges()
    {
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidPasswordException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWrongPassword_ShouldNotRevokeTokens()
    {
        _encryptionService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidPasswordException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));

        await _refreshTokenRepository.DidNotReceive().RevokeAllByUserIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
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
}