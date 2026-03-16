using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.ValueObjects;
using MediatR;

namespace LinkVault.Application.Collections.Commands;

public class CreateCollectionHandler(
    ICollectionRepository collectionRepository,
    ICollectionQueries collectionQueries,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCollectionCommand, CollectionDto>
{
    public async Task<CollectionDto> Handle(
        CreateCollectionCommand command,
        CancellationToken cancellationToken)
    {
        var slug = await GenerateUniqueSlugAsync(command.Name, cancellationToken);

        var tags = command.FilterTags.Select(t => new Tag(t)).ToList();
        var collection = CollectionEntity.Create(
            currentUser.UserId,
            command.Name,
            slug,
            tags,
            command.IsPublic);

        await collectionRepository.AddAsync(collection, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var collections = await collectionQueries.GetByUserIdAsync(
            currentUser.UserId, cancellationToken);

        return collections.First(c => c.Slug == slug.Value);
    }

    private async Task<Slug> GenerateUniqueSlugAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var baseSlug = Slug.Generate(name);
        var slug = baseSlug;
        var counter = 1;

        while (await collectionRepository.SlugExistsForUserAsync(
                   currentUser.UserId, slug.Value, cancellationToken))
        {
            slug = new Slug($"{baseSlug.Value}-{++counter}");
        }

        return slug;
    }
}