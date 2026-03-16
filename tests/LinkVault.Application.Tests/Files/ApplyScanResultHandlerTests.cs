using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Files.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Files;

public class ApplyScanResultHandlerTests
{
    private readonly IFileRepository _fileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IQuarantineStorageService _quarantineStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplyScanResultHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _fileId = Guid.NewGuid();
    private const string StoredFileName = "stored-file-name";

    public ApplyScanResultHandlerTests()
    {
        _fileRepository = Substitute.For<IFileRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _quarantineStorage = Substitute.For<IQuarantineStorageService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new ApplyScanResultHandler(
            _userRepository,
            _fileRepository,
            _encryptionService,
            _quarantineStorage,
            _unitOfWork);

        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .Returns(CreatePendingFile(_userId));

        _userRepository
            .FindByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(CreateUser());

        _encryptionService
            .DecryptDek(Arg.Any<string>())
            .Returns("plaintext-dek");

        _encryptionService
            .EncryptBytes(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns((EncryptedContent: [0x04, 0x05], Iv: "test-iv"));

        _quarantineStorage
            .ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([0x01, 0x02, 0x03]);
    }

    private ApplyScanResultCommand ValidCommand(bool isClean) => new(
        FileId: _fileId,
        StoredFileName: StoredFileName,
        IsClean: isClean);

    [Fact]
    public async Task Handle_WhenFileIsClean_ShouldReturnUnit()
    {
        var result = await _handler.Handle(ValidCommand(true), CancellationToken.None);

        result.Message.Should().NotBeNull();
        result.Message.Should().ContainEquivalentOf("scan");
    }

    [Fact]
    public async Task Handle_WhenFileIsClean_ShouldReadFromQuarantine()
    {
        await _handler.Handle(ValidCommand(true), CancellationToken.None);

        await _quarantineStorage.Received(1).ReadAsync(
            Arg.Is<string>(s => s == StoredFileName),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFileIsClean_ShouldDecryptDekFromUser()
    {
        await _handler.Handle(ValidCommand(true), CancellationToken.None);

        _encryptionService.Received(1).DecryptDek(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenFileIsClean_ShouldEncryptContent()
    {
        await _handler.Handle(ValidCommand(true), CancellationToken.None);

        _encryptionService.Received(1).EncryptBytes(
            Arg.Any<byte[]>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenFileIsClean_ShouldMarkFileClean()
    {
        FileAttachmentEntity? capturedFile = null;
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedFile = CreatePendingFile(_userId);
                return capturedFile;
            });

        await _handler.Handle(ValidCommand(true), CancellationToken.None);

        capturedFile!.ScanStatus.Should().Be(FileScanStatus.Clean);
        capturedFile.EncryptedContent.Should().NotBeNull();
        capturedFile.EncryptionIv.Should().Be("test-iv");
    }

    [Fact]
    public async Task Handle_WhenFileIsClean_ShouldDeleteFromQuarantine()
    {
        await _handler.Handle(ValidCommand(true), CancellationToken.None);

        await _quarantineStorage.Received(1).DeleteAsync(
            Arg.Is<string>(s => s == StoredFileName),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFileIsClean_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(true), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFileIsInfected_ShouldMarkFileInfected()
    {
        FileAttachmentEntity? capturedFile = null;
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedFile = CreatePendingFile(_userId);
                return capturedFile;
            });

        await _handler.Handle(ValidCommand(false), CancellationToken.None);

        capturedFile!.ScanStatus.Should().Be(FileScanStatus.Infected);
    }

    [Fact]
    public async Task Handle_WhenFileIsInfected_ShouldNotEncryptContent()
    {
        await _handler.Handle(ValidCommand(false), CancellationToken.None);

        _encryptionService.DidNotReceive().EncryptBytes(
            Arg.Any<byte[]>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenFileIsInfected_ShouldNotLoadUser()
    {
        await _handler.Handle(ValidCommand(false), CancellationToken.None);

        await _userRepository.DidNotReceive().FindByIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFileIsInfected_ShouldDeleteFromQuarantine()
    {
        await _handler.Handle(ValidCommand(false), CancellationToken.None);

        await _quarantineStorage.Received(1).DeleteAsync(
            Arg.Is<string>(s => s == StoredFileName),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFileIsInfected_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(false), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFileNotFound_ShouldThrowResourceNotFoundException()
    {
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(true), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenFileNotFound_ShouldNotSaveChanges()
    {
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(ValidCommand(true), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ShouldStillDeleteFromQuarantine()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new Exception("DB error"));

        await Assert.ThrowsAsync<Exception>(
            () => _handler.Handle(ValidCommand(true), CancellationToken.None));

        await _quarantineStorage.Received(1).DeleteAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private FileAttachmentEntity CreatePendingFile(Guid userId)
        => FileAttachmentEntity.Create(
            Guid.NewGuid(),
            userId,
            "document.pdf",
            StoredFileName,
            "application/pdf",
            1024 * 1024);

    private static UserEntity CreateUser()
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