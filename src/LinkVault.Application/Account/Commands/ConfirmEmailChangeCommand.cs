using MediatR;

namespace LinkVault.Application.Account.Commands;

public record ConfirmEmailChangeCommand(
    string Token
) : IRequest<string>;