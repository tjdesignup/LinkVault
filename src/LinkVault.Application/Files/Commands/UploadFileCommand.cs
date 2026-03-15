using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Files.Commands;

public record UploadFileCommand(
    Guid LinkId,
    string OriginalFileName,
    string MimeType,
    long FileSizeBytes,
    Stream FileStream
) : IRequest<FileAttachmentDto>;