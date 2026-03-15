using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Collections.Queries;

public record GetCollectionsQuery : IRequest<List<CollectionDto>>;