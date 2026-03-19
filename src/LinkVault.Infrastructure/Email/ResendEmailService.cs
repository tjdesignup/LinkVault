using System.Reflection;
using LinkVault.Application.Abstractions;
using Resend;

namespace LinkVault.Infrastructure.Email;

public sealed class ResendEmailService(IResend resend, string fromAddress) : IEmailService
{
    private readonly IResend _resend = resend;
    private readonly string _fromAddress = fromAddress;

    public async Task SendRegistrationConfirmationAsync(
        string toEmail,
        string confirmationLink,
        CancellationToken cancellationToken = default)
    {
        var html = LoadTemplate("RegistrationConfirmation")
            .Replace("{{CONFIRMATION_LINK}}", confirmationLink);

        await SendAsync(toEmail, "Vítejte v LinkVault — potvrďte svůj email", html, cancellationToken);
    }

    public async Task SendEmailChangeConfirmationAsync(
        string toEmail,
        string confirmationLink,
        CancellationToken cancellationToken = default)
    {
        var html = LoadTemplate("EmailChangeConfirmation")
            .Replace("{{CONFIRMATION_LINK}}", confirmationLink);

        await SendAsync(toEmail, "Potvrzení změny emailové adresy — LinkVault", html, cancellationToken);
    }

    public async Task SendPromotionalAsync(
        string toEmail,
        string subject,
        CancellationToken cancellationToken = default)
    {
        var html = LoadTemplate("Promotional")
            .Replace("{{SUBJECT}}", subject)
            .Replace("{{BADGE_TEXT}}", "Novinka pro uživatele Pro")
            .Replace("{{BODY_TEXT}}", "Základní plán vám slouží dobře — ale s LinkVault Pro dostanete nástroje, které vaši produktivitu posunou na další úroveň.")
            .Replace("{{CTA_LINK}}", "https://linkvault.app/subscription/upgrade")
            .Replace("{{CTA_TEXT}}", "Přejít na Pro — 99 Kč/měs")
            .Replace("{{UNSUBSCRIBE_LINK}}", "https://linkvault.app/unsubscribe");

        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new EmailMessage
        {
            From = _fromAddress,
            To = { toEmail },
            Subject = subject,
            HtmlBody = htmlBody
        };

        await _resend.EmailSendAsync(message, ct);
    }

    private static string LoadTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"LinkVault.Infrastructure.Email.Templates.{templateName}.html";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Email template '{templateName}' not found as embedded resource.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}