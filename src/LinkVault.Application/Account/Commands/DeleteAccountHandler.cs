using LinkVault.Application.Abstractions;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Account.Commands;

public class DeleteAccountHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IEncryptionService encryptionService,
    ICurrentUser currentUser)
    : IRequestHandler<DeleteAccountCommand, string>
{
    public async Task<string> Handle(
        DeleteAccountCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (!encryptionService.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            throw new InvalidPasswordException();

        user.Delete();

        await refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Account deleted successfully.";
    }
}