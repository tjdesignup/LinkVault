using FluentAssertions;
using LinkVault.Application.Abstractions;
using LinkVault.Application.Files.Events;
using LinkVault.Application.DTOs;
using LinkVault.Application.Files.Commands;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;
using LinkVault.Domain.ValueObjects;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace LinkVault.Application.Tests.Files;

public class UploadFileHandlerTests
{
    private readonly IFileRepository _fileRepository;
    private readonly ILinkRepository _linkRepository;
    private readonly IQuarantineStorageService _quarantineStorage;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UploadFileHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _linkId = Guid.NewGuid();

    public UploadFileHandlerTests()
    {
        _fileRepository = Substitute.For<IFileRepository>();
        _linkRepository = Substitute.For<ILinkRepository>();
        _quarantineStorage = Substitute.For<IQuarantineStorageService>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _currentUser = Substitute.For<ICurrentUser>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new UploadFileHandler(
            _fileRepository,
            _linkRepository,
            _quarantineStorage,
            _eventPublisher,
            _currentUser,
            _unitOfWork);

        _currentUser.UserId.Returns(_userId);
        _fileRepository
            .CountByLinkIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(0);
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(CreateLink(_userId));
    }

    private UploadFileCommand ValidCommand() => new(
        LinkId: _linkId,
        OriginalFileName: "document.pdf",
        MimeType: "application/pdf",
        FileSizeBytes: 1024 * 1024,
        FileStream: new MemoryStream([0x01, 0x02, 0x03]));

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnFileAttachmentDto()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<FileAttachmentDto>();
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveFileToQuarantine()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _quarantineStorage.Received(1).SaveAsync(
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldAddFileToRepository()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _fileRepository.Received(1).AddAsync(
            Arg.Is<FileAttachmentEntity>(f =>
                f.LinkId == _linkId &&
                f.UserId == _userId &&
                f.OriginalFileName == "document.pdf" &&
                f.MimeType == "application/pdf"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldPublishFileUploadedEvent()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            Arg.Any<FileUploadedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveChanges()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldPublishEventAfterSave()
    {
        var saveOrder = 0;
        var publishOrder = 0;
        var counter = 0;

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { saveOrder = ++counter; return 1; });

        _eventPublisher.PublishAsync(Arg.Any<FileUploadedEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ => { publishOrder = ++counter; return Task.CompletedTask; });

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        saveOrder.Should().BeLessThan(publishOrder);
    }

    [Fact]
    public async Task Handle_WhenLinkNotFound_ShouldThrowResourceNotFoundException()
    {
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenLinkBelongsToAnotherUser_ShouldThrowResourceForbiddenException()
    {
        _linkRepository
            .FindByIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(CreateLink(Guid.NewGuid()));

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenAttachmentLimitReached_ShouldThrowFileAttachmentLimitExceededException()
    {
        _fileRepository
            .CountByLinkIdAsync(_linkId, Arg.Any<CancellationToken>())
            .Returns(3);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<FileAttachmentLimitExceededException>();
    }

    [Fact]
    public async Task Handle_WhenFileTooLarge_ShouldThrowFileTooLargeException()
    {
        var command = ValidCommand() with
        {
            FileSizeBytes = 25L * 1024 * 1024 + 1
        };

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FileTooLargeException>();
    }

    private static LinkEntity CreateLink(Guid userId)
        => LinkEntity.Create(
            userId,
            new Url("https://github.com"),
            "Test Link",
            null,
            []);
}