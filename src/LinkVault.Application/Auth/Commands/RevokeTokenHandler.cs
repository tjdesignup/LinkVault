using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Auth.Commands;

public class RevokeTokenHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ITokenService tokenService)
    : IRequestHandler<RevokeTokenCommand, MessageDto>
    {
    public async Task<MessageDto> Handle(
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

        return new MessageDto("Device was logged out successfully.");
    }
}