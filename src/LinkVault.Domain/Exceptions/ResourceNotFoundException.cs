namespace LinkVault.Domain.Exceptions;
public sealed class ResourceNotFoundException(string resourceName, Guid id)
    : DomainException($"{resourceName} with id '{id}' was not found.");