using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Files.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Files;

public class DeleteFileHandlerTests
{
    private readonly IFileRepository _fileRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DeleteFileHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _fileId = Guid.NewGuid();

    public DeleteFileHandlerTests()
    {
        _fileRepository = Substitute.For<IFileRepository>();
        _currentUser = Substitute.For<ICurrentUser>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new DeleteFileHandler(
            _fileRepository,
            _currentUser,
            _unitOfWork);

        _currentUser.UserId.Returns(_userId);
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .Returns(CreateFile(_userId));
    }

    private DeleteFileCommand ValidCommand() => new(_fileId);

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnUnit()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Message.Should().NotBeNull();
        result.Message.Should().Contain("deleted");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSoftDeleteFile()
    {
        FileAttachmentEntity? capturedFile = null;
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedFile = CreateFile(_userId);
                return capturedFile;
            });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedFile!.IsDeleted.Should().BeTrue();
        capturedFile.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFileNotFound_ShouldThrowResourceNotFoundException()
    {
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenFileBelongsToAnotherUser_ShouldThrowResourceForbiddenException()
    {
        _fileRepository
            .FindByIdAsync(_fileId, Arg.Any<CancellationToken>())
            .Returns(CreateFile(Guid.NewGuid()));

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceForbiddenException>();
    }

    private static FileAttachmentEntity CreateFile(Guid userId)
        => FileAttachmentEntity.Create(
            Guid.NewGuid(),
            userId,
            "document.pdf",
            Guid.NewGuid().ToString("N"),
            "application/pdf",
            1024 * 1024);
}