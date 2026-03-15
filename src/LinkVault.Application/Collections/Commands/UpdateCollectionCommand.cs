using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Collections.Commands;

public record UpdateCollectionCommand(
    Guid CollectionId,
    string Name,
    List<string> FilterTags,
    bool IsPublic
) : IRequest<CollectionDto>;