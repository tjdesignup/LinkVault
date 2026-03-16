using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Collections.Commands;

public record DeleteCollectionCommand(
    Guid CollectionId
) : IRequest<MessageDto>;