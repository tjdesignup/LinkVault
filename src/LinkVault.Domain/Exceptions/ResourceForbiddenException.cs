namespace LinkVault.Domain.Exceptions;
public sealed class ResourceForbiddenException(string resourceName)
    : DomainException($"Access to {resourceName} is forbidden.");