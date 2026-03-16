using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Files.Commands;

public class ApplyScanResultHandler(
    IUserRepository userRepository,
    IFileRepository fileRepository,
    IEncryptionService encryptionService,
    IQuarantineStorageService quarantineStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ApplyScanResultCommand, MessageDto>
{
    public async Task<MessageDto> Handle(
        ApplyScanResultCommand command,
        CancellationToken cancellationToken)
    {
        var file = await fileRepository.FindByIdAsync(command.FileId, cancellationToken)
            ?? throw new ResourceNotFoundException("File", command.FileId);

        try
        {
            if (command.IsClean)
            {
                var user = await userRepository.FindByIdAsync(file.UserId, cancellationToken)
                    ?? throw new ResourceNotFoundException("User", file.UserId);

                var rawBytes = await quarantineStorage.ReadAsync(
                    command.StoredFileName, cancellationToken);

                var plaintextDek = encryptionService.DecryptDek(user.EncryptedDek);
                var (encryptedContent, iv) = encryptionService.EncryptBytes(rawBytes, plaintextDek);

                file.MarkClean(encryptedContent, iv);
            }
            else
            {
                file.MarkInfected();
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            await quarantineStorage.DeleteAsync(command.StoredFileName, cancellationToken);
        }

        return new MessageDto("Scan result applied successfully.");
    }
}