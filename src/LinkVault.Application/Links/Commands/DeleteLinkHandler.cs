using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Links.Commands;

public class DeleteLinkHandler(
    ILinkRepository linkRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteLinkCommand, MessageDto>
{
    public async Task<MessageDto> Handle(
        DeleteLinkCommand command,
        CancellationToken cancellationToken)
    {
        var link = await linkRepository.FindByIdAsync(command.LinkId, cancellationToken)
            ?? throw new ResourceNotFoundException("Link", command.LinkId);

        if (link.UserId != currentUser.UserId)
            throw new ResourceForbiddenException("Link");

        link.Delete();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageDto("Link was deleted");
    }
}