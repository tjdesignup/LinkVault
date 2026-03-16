using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Auth.Commands;

public record ConfirmEmailCommand(
    string Token
) : IRequest<MessageDto>;