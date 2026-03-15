using MediatR;

namespace LinkVault.Application.Auth.Commands;

public record ConfirmEmailCommand(
    string Token
) : IRequest<string>;