namespace LinkVault.Domain.Exceptions;

public sealed class InvalidLoginException()
    : DomainException("Login was incorrect.");