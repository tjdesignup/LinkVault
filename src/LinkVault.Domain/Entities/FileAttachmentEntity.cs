using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;

namespace LinkVault.Domain.Entities;

public class FileAttachmentEntity : BaseEntity
{
    private const int MaxFileSizeMb = 25;
    private const long MaxFileSizeBytes = MaxFileSizeMb * 1024L * 1024L;

    public Guid LinkId { get; private set; }
    public Guid UserId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StoredFileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public FileScanStatus ScanStatus { get; private set; }
    public byte[]? EncryptedContent { get; private set; }
    public string? EncryptionIv { get; private set; }
    public DateTime? ScanCompletedAt { get; private set; }

    public bool IsAvailable => ScanStatus == FileScanStatus.Clean;

    private FileAttachmentEntity() { }
    private FileAttachmentEntity(
        Guid linkId,
        Guid userId,
        string originalFileName,
        string storedFileName,
        string mimeType,
        long fileSizeBytes)
    {
        LinkId = linkId;
        UserId = userId;
        OriginalFileName = originalFileName;
        StoredFileName = storedFileName;
        MimeType = mimeType;
        FileSizeBytes = fileSizeBytes;
        ScanStatus = FileScanStatus.Pending;
    }

    public static FileAttachmentEntity Create(
        Guid linkId,
        Guid userId,
        string originalFileName,
        string storedFileName,
        string mimeType,
        long fileSizeBytes)
    {
        if (linkId == Guid.Empty)
            throw new ArgumentException("LinkId cannot be empty.", nameof(linkId));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original file name cannot be empty.", nameof(originalFileName));

        if (string.IsNullOrWhiteSpace(storedFileName))
            throw new ArgumentException("Stored file name cannot be empty.", nameof(storedFileName));

        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException("MIME type cannot be empty.", nameof(mimeType));

        if (fileSizeBytes <= 0)
            throw new ArgumentException("File size must be greater than zero.", nameof(fileSizeBytes));

        if (fileSizeBytes > MaxFileSizeBytes)
            throw new FileTooLargeException(MaxFileSizeMb);

        return new FileAttachmentEntity(linkId, userId, originalFileName, storedFileName, mimeType, fileSizeBytes);
    }

    public void MarkClean(byte[] encryptedContent, string encryptionIv)
    {
        if (ScanStatus != FileScanStatus.Pending)
            throw new InvalidOperationException("File has already been scanned.");

        if (encryptedContent is null || encryptedContent.Length == 0)
            throw new ArgumentException("Encrypted content cannot be empty.", nameof(encryptedContent));

        if (string.IsNullOrWhiteSpace(encryptionIv))
            throw new ArgumentException("Encryption IV cannot be empty.", nameof(encryptionIv));

        ScanStatus = FileScanStatus.Clean;
        EncryptedContent = encryptedContent;
        EncryptionIv = encryptionIv;
        ScanCompletedAt = DateTime.UtcNow;
    }

    public void MarkInfected()
    {
        if (ScanStatus != FileScanStatus.Pending)
            throw new InvalidOperationException("File has already been scanned.");

        ScanStatus = FileScanStatus.Infected;
        ScanCompletedAt = DateTime.UtcNow;
    }

    public void MarkScanFailed()
    {
        if (ScanStatus != FileScanStatus.Pending)
            throw new InvalidOperationException("File has already been scanned.");

        ScanStatus = FileScanStatus.Failed;
        ScanCompletedAt = DateTime.UtcNow;
    }

    public void Delete() => SoftDelete();
}