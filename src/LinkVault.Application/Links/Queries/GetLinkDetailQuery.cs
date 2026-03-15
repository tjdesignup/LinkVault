using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Links.Queries;

public record GetLinkDetailQuery(
    Guid LinkId
) : IRequest<LinkDto?>;