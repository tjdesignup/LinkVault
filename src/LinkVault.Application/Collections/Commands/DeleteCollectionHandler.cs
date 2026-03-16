using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Collections.Commands;

public class DeleteCollectionHandler(
    ICollectionRepository collectionRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCollectionCommand, MessageDto>
{
    public async Task<MessageDto> Handle(
        DeleteCollectionCommand command,
        CancellationToken cancellationToken)
    {
        var collection = await collectionRepository.FindByIdAsync(
            command.CollectionId, cancellationToken)
            ?? throw new ResourceNotFoundException("Collection", command.CollectionId);

        if (collection.UserId != currentUser.UserId)
            throw new ResourceForbiddenException("Collection");

        collection.Delete();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageDto("Collection deleted successfully.");
    }
}