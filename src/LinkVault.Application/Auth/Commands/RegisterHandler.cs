using LinkVault.Application.Abstractions;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Abstractions;
using MediatR;

namespace LinkVault.Application.Auth.Commands;

public class RegisterHandler(
    IUserRepository userRepository,
    IEmailConfirmationTokenRepository emailTokenRepository,
    ICurrentSubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork,
    IEncryptionService encryptionService,
    IEmailService emailService)
    : IRequestHandler<RegisterCommand, string>
{
    public async Task<string> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var emailNormalized = command.Email.Trim().ToLowerInvariant();
        var blindHash = encryptionService.ComputeBlindIndexHash(emailNormalized);

        var existing = await userRepository.FindByEmailBlindIndexHashAsync(
            blindHash, cancellationToken);

        if (existing is not null)
            throw new EmailAlreadyInUseException();

        var passwordHash = encryptionService.HashPassword(command.Password);

        var encryptedDek = encryptionService.GenerateEncryptedDek();
        var plaintextDek = encryptionService.DecryptDek(encryptedDek);

        var emailEncrypted = encryptionService.Encrypt(emailNormalized, plaintextDek);
        var firstNameEncrypted = encryptionService.Encrypt(command.FirstName, plaintextDek);
        var surnameEncrypted = encryptionService.Encrypt(command.Surname, plaintextDek);

        var user = UserEntity.Register(
            emailEncrypted: emailEncrypted,
            emailBlindIndex: blindHash,
            firstNameEncrypted: firstNameEncrypted,
            surNameEncrypted: surnameEncrypted,
            passwordHash: passwordHash,
            encryptedDek: encryptedDek);

        await userRepository.AddAsync(user, cancellationToken);

        var subscription = CurrentSubscriptionEntity.CreateFree(user.Id, $"pending_{user.Id}");
        await subscriptionRepository.AddAsync(subscription, cancellationToken);

        var plainToken = Guid.NewGuid().ToString("N");
        var confirmationToken = EmailConfirmationTokenEntity.Create(
            user.Id,
            plainToken,
            ConfirmationTokenType.Registration);

        await emailTokenRepository.AddAsync(confirmationToken, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var confirmationLink = $"https://linkvault.app/confirm-email?token={plainToken}";
        await emailService.SendRegistrationConfirmationAsync(
            emailNormalized,
            confirmationLink,
            cancellationToken);

        return "Registrace proběhla úspěšně. Zkontrolujte svůj email a potvrďte účet.";
    }
}