namespace LinkVault.Domain.Exceptions;

public sealed class UserDeletedException()
    : DomainException("User account has been deleted.");