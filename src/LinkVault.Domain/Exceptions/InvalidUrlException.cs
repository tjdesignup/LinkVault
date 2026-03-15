namespace LinkVault.Domain.Exceptions;

public sealed class InvalidUrlException(string url)
    : DomainException($"'{url}' is not a valid URL. Only http and https are allowed.");