namespace LinkVault.Domain.Exceptions;

public sealed class InvalidPasswordException()
    : DomainException("Password is incorrect.");