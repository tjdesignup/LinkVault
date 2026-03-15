using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Account.Commands;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Account;

public class UpdateProfileHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly ICurrentUser _currentUser;
    private readonly UpdateProfileHandler _handler;

    public UpdateProfileHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new UpdateProfileHandler(
            _userRepository,
            _unitOfWork,
            _encryptionService,
            _currentUser);

        _currentUser.UserId.Returns(Guid.NewGuid());
        _encryptionService.DecryptDek(Arg.Any<string>()).Returns("plaintext-dek");
        _encryptionService.Encrypt(Arg.Any<string>(), Arg.Any<string>()).Returns("encrypted-value");
        _encryptionService.Decrypt(Arg.Any<string>(), Arg.Any<string>()).Returns("decrypted-value");
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfirmedUser());
    }

    private static UpdateProfileCommand ValidCommand() => new(
        FirstName: "Nové Jméno",
        Surname: "Nové Příjmení");

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnUserDto()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<UserDto>();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldUpdateEncryptedFields()
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

        capturedUser!.FirstNameEncrypted.Should().Be("encrypted-value");
        capturedUser.SurNameEncrypted.Should().Be("encrypted-value");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldEncryptBothFields()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        // FirstName + Surname — dvě volání Encrypt
        _encryptionService.Received(2).Encrypt(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
            firstNameEncrypted: "old-first-name-encrypted",
            surNameEncrypted: "old-surname-encrypted",
            passwordHash: "hashed-password",
            encryptedDek: "dek");
        user.ConfirmEmail();
        return user;
    }
}