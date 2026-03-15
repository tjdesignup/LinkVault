using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.ValueObjects;
using MediatR;

namespace LinkVault.Application.Links.Commands;

public class UpdateLinkHandler(
    ILinkRepository linkRepository,
    ILinkQueries linkQueries,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateLinkCommand, LinkDto>
{
    public async Task<LinkDto> Handle(
        UpdateLinkCommand command,
        CancellationToken cancellationToken)
    {
        var link = await linkRepository.FindByIdAsync(command.LinkId, cancellationToken)
            ?? throw new InvalidOperationException("Link not found.");

        if (link.UserId != currentUser.UserId)
            throw new InvalidOperationException("Link belongs to another user.");

        var tags = command.Tags.Select(t => new Tag(t)).ToList();
        link.UpdateDetails(new Url(command.Url), command.Title, command.Note, tags);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await linkQueries.GetByIdAsync(link.Id, currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Link not found after update.");
    }
}