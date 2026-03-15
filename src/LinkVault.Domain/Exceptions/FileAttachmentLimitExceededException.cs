namespace LinkVault.Domain.Exceptions;

public sealed class FileAttachmentLimitExceededException(int limit)
    : DomainException($"Maximum of {limit} file attachments per link has been reached.");