using LinkVault.Application.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Files.Queries;

public class DownloadFileHandler(
    IFileRepository fileRepository,
    IUserRepository userRepository,
    IEncryptionService encryptionService,
    ICurrentUser currentUser)
    : IRequestHandler<DownloadFileQuery, (byte[] Content, string FileName, string MimeType)>
{
    public async Task<(byte[] Content, string FileName, string MimeType)> Handle(
        DownloadFileQuery query,
        CancellationToken cancellationToken)
    {
        var file = await fileRepository.FindByIdAsync(query.FileId, cancellationToken)
            ?? throw new ResourceNotFoundException("File", query.FileId);

        if (file.UserId != currentUser.UserId)
            throw new ResourceForbiddenException("File");

        if (!file.IsAvailable)
            throw new FileNotAvailableException();

        var user = await userRepository.FindByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new ResourceNotFoundException("User", currentUser.UserId);

        var plaintextDek = encryptionService.DecryptDek(user.EncryptedDek);
        var decryptedContent = encryptionService.DecryptBytes(
            file.EncryptedContent!,
            file.EncryptionIv!,
            plaintextDek);

        return (decryptedContent, file.OriginalFileName, file.MimeType);
    }
}