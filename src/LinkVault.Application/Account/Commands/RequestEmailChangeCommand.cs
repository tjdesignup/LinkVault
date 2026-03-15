using MediatR;

namespace LinkVault.Application.Account.Commands;

public record RequestEmailChangeCommand(
    string NewEmail,
    string CurrentPassword
) : IRequest<string>;