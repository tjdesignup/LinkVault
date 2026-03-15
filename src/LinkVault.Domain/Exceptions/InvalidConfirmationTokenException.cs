namespace LinkVault.Domain.Exceptions;

public sealed class InvalidConfirmationTokenException()
    : DomainException("Confirmation token is invalid or expired.");