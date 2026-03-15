using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Application.Mappings;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Abstractions;
using MediatR;

namespace LinkVault.Application.Auth.Commands;

public class LoginHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ICurrentSubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork,
    IEncryptionService encryptionService,
    ITokenService tokenService,
    IBruteForceProtectionService bruteForce)
    : IRequestHandler<LoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var emailNormalized = command.Email.Trim().ToLowerInvariant();
        var blindHash = encryptionService.ComputeBlindIndexHash(emailNormalized);

        var user = await userRepository.FindByEmailBlindIndexHashAsync(
            blindHash, cancellationToken) ?? throw new InvalidLoginException();

        if (user.IsDeleted)
            throw new UserDeletedException();

        if (!user.EmailConfirmed)
            throw new EmailNotConfirmedException();

        var isLocked = await bruteForce.IsLockedAsync(user.Id, cancellationToken);
        if (isLocked)
        {
            var remaining = await bruteForce.GetRemainingLockTimeAsync(user.Id, cancellationToken);
            throw new AccountLockedException(remaining);
        }

        var isPasswordValid = encryptionService.VerifyPassword(command.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            var isNowLocked = await bruteForce.RecordFailedAttemptAsync(user.Id, cancellationToken);
            if (isNowLocked)
                await refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken);

            throw new InvalidLoginException();
        }

        await bruteForce.ResetAsync(user.Id, cancellationToken);

        var subscription = await subscriptionRepository.FindByUserIdAsync(user.Id, cancellationToken);
        var isProTier = subscription?.IsProActive ?? false;

        var userRole = user.IsAdmin ? "Admin" : "User";

        var accessToken = tokenService.GenerateAccessToken(user.Id, isProTier, userRole);

        var plainRefreshToken = tokenService.GenerateRefreshToken();
        var refreshTokenHash = tokenService.HashRefreshToken(plainRefreshToken);

        var refreshToken = RefreshTokenEntity.Create(
            user.Id,
            refreshTokenHash,
            command.DeviceName,
            command.IpAddress);

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var userDto = user.ToDto(encryptionService) with
        {
            Tier = isProTier ? "Pro" : "Free"
        };

        return new AuthResultDto(accessToken, plainRefreshToken, userDto);
    }
}