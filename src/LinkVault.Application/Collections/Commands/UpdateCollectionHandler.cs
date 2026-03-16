using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.ValueObjects;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Collections.Commands;

public class UpdateCollectionHandler(
    ICollectionRepository collectionRepository,
    ICollectionQueries collectionQueries,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCollectionCommand, CollectionDto>
{
    public async Task<CollectionDto> Handle(
        UpdateCollectionCommand command,
        CancellationToken cancellationToken)
    {
        var collection = await collectionRepository.FindByIdAsync(
            command.CollectionId, cancellationToken)
            ?? throw new ResourceNotFoundException("Collection", command.CollectionId);

        if (collection.UserId != currentUser.UserId)
            throw new ResourceForbiddenException("Collection");

        var newSlug = collection.Name != command.Name
            ? await GenerateUniqueSlugAsync(command.Name, collection.Slug.Value, cancellationToken)
            : collection.Slug;

        var tags = command.FilterTags.Select(t => new Tag(t)).ToList();
        collection.UpdateDetails(command.Name, newSlug, tags, command.IsPublic);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var collections = await collectionQueries.GetByUserIdAsync(
            currentUser.UserId, cancellationToken);

        return collections.First(c => c.Id == collection.Id);
    }

    private async Task<Slug> GenerateUniqueSlugAsync(
        string name,
        string currentSlug,
        CancellationToken cancellationToken)
    {
        var baseSlug = Slug.Generate(name);
        var slug = baseSlug;
        var counter = 1;

        while (await collectionRepository.SlugExistsForUserAsync(
                   currentUser.UserId, slug.Value, cancellationToken)
               && slug.Value != currentSlug)
        {
            slug = new Slug($"{baseSlug.Value}-{++counter}");
        }

        return slug;
    }
}