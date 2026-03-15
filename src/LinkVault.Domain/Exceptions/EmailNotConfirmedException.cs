namespace LinkVault.Domain.Exceptions;

public sealed class EmailNotConfirmedException()
    : DomainException("Email is not confirmed.");