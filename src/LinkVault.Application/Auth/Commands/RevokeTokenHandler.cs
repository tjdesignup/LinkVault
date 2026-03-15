using LinkVault.Application.Abstractions;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Auth.Commands;

public class RevokeTokenHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ITokenService tokenService)
    : IRequestHandler<RevokeTokenCommand, Unit>
    {
    public async Task<Unit> Handle(
        RevokeTokenCommand command,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashRefreshToken(command.RefreshToken);
        var refreshToken = await refreshTokenRepository.FindByTokenHashAsync(
            tokenHash, cancellationToken)
            ?? throw new InvalidTokenException();

        if (command.RevokeAll)
        {
            await refreshTokenRepository.RevokeAllByUserIdAsync(
                refreshToken.UserId, cancellationToken);
        }
        else
        {
            refreshToken.Revoke();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}