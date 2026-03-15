namespace LinkVault.Domain.Exceptions;

public sealed class EmailAlreadyConfirmedException()
    : DomainException("Email is already confirmed.");