using MediatR;

namespace LinkVault.Application.Files.Commands;

public record DeleteFileCommand(
    Guid FileId
) : IRequest<Unit>;