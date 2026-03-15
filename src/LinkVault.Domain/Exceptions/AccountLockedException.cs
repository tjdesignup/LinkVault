namespace LinkVault.Domain.Exceptions;

public sealed class AccountLockedException(TimeSpan remaining)
    : DomainException($"Account is temporarily locked. Try again in {(int)remaining.TotalSeconds} seconds.");