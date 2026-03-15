namespace LinkVault.Domain.Exceptions;

public sealed class FileInfectedException()
    : DomainException("File was rejected because it contains a threat detected by antivirus scan.");