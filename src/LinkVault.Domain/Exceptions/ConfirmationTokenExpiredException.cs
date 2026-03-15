namespace LinkVault.Domain.Exceptions;

public sealed class ConfirmationTokenExpiredException()
    : DomainException("Confirmation token is expired.");