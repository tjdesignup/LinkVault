using MediatR;

namespace LinkVault.Application.Files.Commands;

public record ApplyScanResultCommand(
    Guid FileId,
    string StoredFileName,
    bool IsClean
) : IRequest<Unit>;