namespace LinkVault.Domain.Exceptions;

public sealed class LinkLimitExceededException(int limit)
    : DomainException($"Link limit of {limit} has been reached. Upgrade to Pro for unlimited links.");