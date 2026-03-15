using MediatR;

namespace LinkVault.Application.Auth.Commands;

public record RevokeTokenCommand(
    string RefreshToken,
    bool RevokeAll = false
) : IRequest<Unit>;