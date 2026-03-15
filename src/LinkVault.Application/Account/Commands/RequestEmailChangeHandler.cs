using LinkVault.Application.Abstractions;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;
using MediatR;

namespace LinkVault.Application.Account.Commands;

public class RequestEmailChangeHandler(
    IUserRepository userRepository,
    IEmailConfirmationTokenRepository tokenRepository,
    IUnitOfWork unitOfWork,
    IEncryptionService encryptionService,
    IEmailService emailService,
    ICurrentUser currentUser)
    : IRequestHandler<RequestEmailChangeCommand, string>
{
    public async Task<string> Handle(
        RequestEmailChangeCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (!encryptionService.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            throw new InvalidPasswordException();

        var newEmailNormalized = command.NewEmail.Trim().ToLowerInvariant();
        var newBlindHash = encryptionService.ComputeBlindIndexHash(newEmailNormalized);

        if (newBlindHash == user.EmailBlindIndexHash)
            throw new EmailAlreadyConfirmedException();

        var existing = await userRepository.FindByEmailBlindIndexHashAsync(
            newBlindHash, cancellationToken);

        if (existing is not null)
            throw new EmailAlreadyInUseException();

        var plaintextDek = encryptionService.DecryptDek(user.EncryptedDek);
        var newEmailEncrypted = encryptionService.Encrypt(newEmailNormalized, plaintextDek);

        user.RequestEmailChange(newEmailEncrypted, newBlindHash);

        await tokenRepository.InvalidateExistingAsync(
            user.Id,
            ConfirmationTokenType.EmailChange,
            cancellationToken);

        var plainToken = Guid.NewGuid().ToString("N");
        var confirmationToken = EmailConfirmationTokenEntity.Create(
            user.Id,
            plainToken,
            ConfirmationTokenType.EmailChange);

        await tokenRepository.AddAsync(confirmationToken, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var confirmationLink = $"https://linkvault.app/confirm-email-change?token={plainToken}";
        await emailService.SendEmailChangeConfirmationAsync(
            newEmailNormalized,
            confirmationLink,
            cancellationToken);

        return "Email change requested successfully";
    }
}