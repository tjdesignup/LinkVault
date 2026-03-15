using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Links.Commands;

public record AddLinkCommand(
    string Url,
    string? Title,
    string? Note,
    List<string> Tags
) : IRequest<LinkDto>;