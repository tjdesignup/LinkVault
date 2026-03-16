using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Links.Commands;

public record DeleteLinkCommand(
    Guid LinkId
) : IRequest<MessageDto>;