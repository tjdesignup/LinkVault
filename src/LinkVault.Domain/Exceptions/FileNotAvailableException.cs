namespace LinkVault.Domain.Exceptions;

public sealed class FileNotAvailableException()
    : DomainException("File is not available. It may still be pending scan or the scan may have failed.");