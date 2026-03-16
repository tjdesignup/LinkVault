using LinkVault.Application.Abstractions;
using LinkVault.Application.DTOs;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Account.Commands;

public class ChangePasswordHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IEncryptionService encryptionService,
    ICurrentUser currentUser)
    : IRequestHandler<ChangePasswordCommand, MessageDto>
{
    public async Task<MessageDto> Handle(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new ResourceNotFoundException("UserEntity", currentUser.UserId);

        if (!encryptionService.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            throw new InvalidPasswordException();

        if (encryptionService.VerifyPassword(command.NewPassword, user.PasswordHash))
            throw new InvalidPasswordException();

        var newHash = encryptionService.HashPassword(command.NewPassword);
        user.ChangePassword(newHash);

        await refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageDto("Password was changed successfully");
    }
}