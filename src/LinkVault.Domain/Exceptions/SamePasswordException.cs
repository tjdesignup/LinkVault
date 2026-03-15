namespace LinkVault.Domain.Exceptions;

public sealed class SamePasswordException()
    : DomainException("New password cannot be same as current.");