namespace LinkVault.Domain.Exceptions;

public sealed class ProTierRequiredException(string feature)
    : DomainException($"'{feature}' requires a Pro subscription.");