namespace LinkVault.Domain.Exceptions;

public sealed class InvalidRegisterException(string message)
    : DomainException(message ?? "Invalid registration.");