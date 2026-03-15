using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;
using LinkVault.Domain.Abstractions.IRepositories;
using LinkVault.Domain.Abstractions;
using MediatR;
using LinkVault.Application.Abstractions;
using LinkVault.Domain.Entities;
using System.ComponentModel.Design;
using LinkVault.Domain.ValueObjects;

namespace LinkVault.Application.Auth.Commands;

public class ConfirmEmailHandler(
    IEmailConfirmationTokenRepository tokenRepository,
    IUserRepository userRepository,
    IEncryptionService encryptionService,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : IRequestHandler<ConfirmEmailCommand, string>
{
    public async Task<string> Handle(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken)
    {
        var token = await tokenRepository.FindByTokenAsync(
            command.Token,
            ConfirmationTokenType.Registration,
            cancellationToken) ?? throw new InvalidConfirmationTokenException();
        
        try
        {
            token.Use();
        }
        catch (InvalidConfirmationTokenException)
        {
            throw new InvalidConfirmationTokenException();
        }
        catch (ConfirmationTokenExpiredException)
        {   
            await tokenRepository.DeleteAsync(token, cancellationToken);

            var newToken = EmailConfirmationTokenEntity.Create(
                token.UserId,
                Guid.NewGuid().ToString(),
                ConfirmationTokenType.Registration);

            await tokenRepository.AddAsync(newToken, cancellationToken);

            var userForConfirmation = await userRepository.FindByIdAsync(token.UserId, cancellationToken) ?? throw new InvalidRegisterException("User with this email is not registered.");
            
            var plainDek = encryptionService.DecryptDek(userForConfirmation.EncryptedDek);
            var confirmationEmail = encryptionService.Decrypt(userForConfirmation.EmailEncrypted, plainDek);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var confirmationLink = $"https://linkvault.app/confirm-email?token={newToken.Token}";
            await emailService.SendRegistrationConfirmationAsync(
                confirmationEmail,
                confirmationLink,
                cancellationToken);
                
            throw new ConfirmationTokenExpiredException();
        }

        var user = await userRepository.FindByIdAsync(token.UserId, cancellationToken) ?? throw new InvalidRegisterException("User with this email is not registered.");
        user.ConfirmEmail();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Email byl úspěšně potvrzen. Nyní se můžete přihlásit.";
    }
}