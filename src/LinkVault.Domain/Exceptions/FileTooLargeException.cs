namespace LinkVault.Domain.Exceptions;

public sealed class FileTooLargeException(int maxSizeMb)
    : DomainException($"File exceeds the maximum allowed size of {maxSizeMb} MB.");