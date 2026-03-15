using LinkVault.Application.Abstractions;
using LinkVault.Domain.Exceptions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Abstractions;
using MediatR;

namespace LinkVault.Application.Auth.Commands;

public class RefreshAccessTokenHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    ICurrentSubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork,
    ITokenService tokenService)
    : IRequestHandler<RefreshAccessTokenCommand, string>
{
    public async Task<string> Handle(
        RefreshAccessTokenCommand command,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashRefreshToken(command.RefreshToken);
        var refreshToken = await refreshTokenRepository.FindByTokenHashAsync(
            tokenHash, cancellationToken) ?? throw new InvalidTokenException();

        if (refreshToken.IsRevoked)
            throw new InvalidTokenException();

        if (refreshToken.IsExpired)
        {
            refreshToken.Revoke();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidTokenException();
        }

        var user = await userRepository.FindByIdAsync(refreshToken.UserId, cancellationToken) ?? throw new InvalidLoginException();
        
        if (user.IsDeleted)
            throw new UserDeletedException();

        var subscription = await subscriptionRepository.FindByUserIdAsync(
            user.Id, cancellationToken);
        var isProTier = subscription?.IsProActive ?? false;

        var userRole = user.IsAdmin ? "Admin" : "User";

        var accessToken = tokenService.GenerateAccessToken(user.Id, isProTier, userRole);

        return new(accessToken);
    }
}