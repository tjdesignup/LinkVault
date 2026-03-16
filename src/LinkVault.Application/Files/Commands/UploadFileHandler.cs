using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Application.Files.Events;
using LinkVault.Application.Mappings;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Files.Commands;

public class UploadFileHandler(
    IFileRepository fileRepository,
    ILinkRepository linkRepository,
    IQuarantineStorageService quarantineStorage,
    IEventPublisher eventPublisher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadFileCommand, FileAttachmentDto>
{
    private const int MaxAttachments = 3;

    public async Task<FileAttachmentDto> Handle(
        UploadFileCommand command,
        CancellationToken cancellationToken)
    {
        var link = await linkRepository.FindByIdAsync(command.LinkId, cancellationToken)
            ?? throw new ResourceNotFoundException("Link", command.LinkId);

        if (link.UserId != currentUser.UserId)
            throw new ResourceForbiddenException("Link");

        var count = await fileRepository.CountByLinkIdAsync(command.LinkId, cancellationToken);
        if (count >= MaxAttachments)
            throw new FileAttachmentLimitExceededException(MaxAttachments);

        var storedFileName = Guid.NewGuid().ToString("N");
        var file = FileAttachmentEntity.Create(
            command.LinkId,
            currentUser.UserId,
            command.OriginalFileName,
            storedFileName,
            command.MimeType,
            command.FileSizeBytes);

        await quarantineStorage.SaveAsync(storedFileName, command.FileStream, cancellationToken);

        await fileRepository.AddAsync(file, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync(
            new FileUploadedEvent(file.Id, storedFileName),
            cancellationToken);

        return file.ToDto();
    }
}