using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Files.Commands;

public class DeleteFileHandler(
    IFileRepository fileRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteFileCommand, MessageDto>
{
    public async Task<MessageDto> Handle(
        DeleteFileCommand command,
        CancellationToken cancellationToken)
    {
        var file = await fileRepository.FindByIdAsync(command.FileId, cancellationToken)
            ?? throw new ResourceNotFoundException("File", command.FileId);

        if (file.UserId != currentUser.UserId)
            throw new ResourceForbiddenException("File");

        file.Delete();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageDto("File deleted successfully.");
    }
}