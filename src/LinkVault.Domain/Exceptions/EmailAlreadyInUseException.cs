namespace LinkVault.Domain.Exceptions;

public sealed class EmailAlreadyInUseException()
    : DomainException("Email is already in use.");