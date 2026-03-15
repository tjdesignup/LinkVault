namespace LinkVault.Domain.Exceptions;

public sealed class InvalidTokenException()
    : DomainException("Invalid access or refresh token.");