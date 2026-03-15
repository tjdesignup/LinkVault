using MediatR;

namespace LinkVault.Application.Files.Queries;

public record DownloadFileQuery(
    Guid FileId
) : IRequest<(byte[] Content, string FileName, string MimeType)>;