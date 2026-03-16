using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Files.Queries;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Files;

public class DownloadFileHandlerTests
{
    private readonly IFileRepository _fileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ICurrentUser _currentUser;
    private readonly DownloadFileHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _fileId = Guid.NewGuid();

    public DownloadFileHandlerTests()
    {
        _fileRepository = Substitute.For<IFileRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _currentUser = Substitute.For<ICurrentUser>();

        _handler = new DownloadFileHandler(
            _fileRepository,
            _userRepository,
            _encryptionService,
            _currentUser);

        _currentUser.UserId.Returns(_userId);

        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .Returns(CreateCleanFile(_userId));

        _userRepository
            .FindByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(CreateUser());

        _encryptionService
            .DecryptDek(Arg.Any<string>())
            .Returns("plaintext-dek");

        _encryptionService
            .DecryptBytes(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 0x01, 0x02, 0x03 });
    }

    private DownloadFileQuery ValidQuery() => new(_fileId);

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldReturnDecryptedContent()
    {
        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.Content.Should().NotBeEmpty();
        result.Content.Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
        result.FileName.Should().Be("document.pdf");
        result.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldDecryptDekFromUser()
    {
        await _handler.Handle(ValidQuery(), CancellationToken.None);

        _encryptionService.Received(1).DecryptDek(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldDecryptFileContent()
    {
        await _handler.Handle(ValidQuery(), CancellationToken.None);

        _encryptionService.Received(1).DecryptBytes(
            Arg.Any<byte[]>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldLoadUserForDek()
    {
        await _handler.Handle(ValidQuery(), CancellationToken.None);

        await _userRepository.Received(1).FindByIdAsync(
            Arg.Is<Guid>(id => id == _userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFileNotFound_ShouldThrowResourceNotFoundException()
    {
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenFileBelongsToAnotherUser_ShouldThrowResourceForbiddenException()
    {
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .Returns(CreateCleanFile(Guid.NewGuid()));

        var act = async () => await _handler.Handle(ValidQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenFileNotAvailable_ShouldThrowFileNotAvailableException()
    {
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .Returns(CreatePendingFile(_userId));

        var act = async () => await _handler.Handle(ValidQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<FileNotAvailableException>();
    }

    [Fact]
    public async Task Handle_WhenFileNotAvailable_ShouldNotDecryptContent()
    {
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .Returns(CreatePendingFile(_userId));

        await Assert.ThrowsAsync<FileNotAvailableException>(
            () => _handler.Handle(ValidQuery(), CancellationToken.None));

        _encryptionService.DidNotReceive().DecryptBytes(
            Arg.Any<byte[]>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    private static FileAttachmentEntity CreateCleanFile(Guid userId)
    {
        var file = FileAttachmentEntity.Create(
            Guid.NewGuid(),
            userId,
            "document.pdf",
            Guid.NewGuid().ToString("N"),
            "application/pdf",
            1024 * 1024);
        file.MarkClean([0x04, 0x05, 0x06], "test-iv");
        return file;
    }

    private static FileAttachmentEntity CreatePendingFile(Guid userId)
        => FileAttachmentEntity.Create(
            Guid.NewGuid(),
            userId,
            "document.pdf",
            Guid.NewGuid().ToString("N"),
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