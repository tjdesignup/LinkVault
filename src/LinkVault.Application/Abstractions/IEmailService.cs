namespace LinkVault.Application.Abstractions;

public interface IEmailService
{
    Task SendRegistrationConfirmationAsync(
        string toEmail,
        string confirmationLink,
        CancellationToken cancellationToken = default);

    Task SendEmailChangeConfirmationAsync(
        string toEmail,
        string confirmationLink,
        CancellationToken cancellationToken = default);
}