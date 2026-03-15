using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Collections.Queries;

public record GetPublicCollectionQuery(
    string Slug
) : IRequest<PublicCollectionDto?>;