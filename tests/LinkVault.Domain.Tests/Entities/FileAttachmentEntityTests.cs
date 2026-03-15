using FluentAssertions;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;

namespace LinkVault.Domain.Tests.Entities;

public class FileAttachmentEntityTests
{
    private static FileAttachmentEntity CreateAttachment(
        Guid? linkId = null,
        Guid? userId = null,
        string originalFileName = "document.pdf",
        string storedFileName = "550e8400-e29b-41d4-a716-446655440000",
        string mimeType = "application/pdf",
        long fileSizeBytes = 1024 * 1024)
        => FileAttachmentEntity.Create(
            linkId ?? Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            originalFileName,
            storedFileName,
            mimeType,
            fileSizeBytes);

    private static byte[] SampleEncryptedContent() => [0x01, 0x02, 0x03, 0x04];

    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        var linkId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var attachment = FileAttachmentEntity.Create(
            linkId, userId, "document.pdf",
            "550e8400-e29b-41d4-a716-446655440000",
            "application/pdf", 1024);

        attachment.Id.Should().NotBe(Guid.Empty);
        attachment.LinkId.Should().Be(linkId);
        attachment.UserId.Should().Be(userId);
        attachment.OriginalFileName.Should().Be("document.pdf");
        attachment.StoredFileName.Should().Be("550e8400-e29b-41d4-a716-446655440000");
        attachment.MimeType.Should().Be("application/pdf");
        attachment.FileSizeBytes.Should().Be(1024);
        attachment.ScanStatus.Should().Be(FileScanStatus.Pending);
        attachment.EncryptedContent.Should().BeNull();
        attachment.EncryptionIv.Should().BeNull();
        attachment.ScanCompletedAt.Should().BeNull();
        attachment.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldInheritFromBaseEntity()
    {
        var attachment = CreateAttachment();
        attachment.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void Create_WhenLinkIdIsEmpty_ShouldThrowArgumentException()
    {
        var act = () => FileAttachmentEntity.Create(
            Guid.Empty, Guid.NewGuid(), "file.pdf", "stored", "application/pdf", 1024);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhenUserIdIsEmpty_ShouldThrowArgumentException()
    {
        var act = () => FileAttachmentEntity.Create(
            Guid.NewGuid(), Guid.Empty, "file.pdf", "stored", "application/pdf", 1024);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenOriginalFileNameIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => FileAttachmentEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), value, "stored", "application/pdf", 1024);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenStoredFileNameIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => FileAttachmentEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), "file.pdf", value, "application/pdf", 1024);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenMimeTypeIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => FileAttachmentEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), "file.pdf", "stored", value, 1024);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WhenFileSizeIsZeroOrNegative_ShouldThrowArgumentException(long value)
    {
        var act = () => FileAttachmentEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), "file.pdf", "stored", "application/pdf", value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhenFileSizeExceeds25MB_ShouldThrowFileTooLargeException()
    {
        var over25Mb = 25L * 1024 * 1024 + 1;

        var act = () => FileAttachmentEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), "file.pdf", "stored", "application/pdf", over25Mb);

        act.Should().Throw<FileTooLargeException>();
    }

    [Fact]
    public void MarkClean_ShouldSetStatusCleanAndPersistEncryptedContent()
    {
        var attachment = CreateAttachment();
        var content = SampleEncryptedContent();

        attachment.MarkClean(content, "base64-iv-here");

        attachment.ScanStatus.Should().Be(FileScanStatus.Clean);
        attachment.EncryptedContent.Should().BeEquivalentTo(content);
        attachment.EncryptionIv.Should().Be("base64-iv-here");
        attachment.ScanCompletedAt.Should().NotBeNull();
        attachment.ScanCompletedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void MarkClean_WhenAlreadyScanned_ShouldThrowInvalidOperationException()
    {
        var attachment = CreateAttachment();
        attachment.MarkClean(SampleEncryptedContent(), "iv");

        var act = () => attachment.MarkClean(SampleEncryptedContent(), "iv");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkClean_WhenEncryptedContentIsEmpty_ShouldThrowArgumentException()
    {
        var attachment = CreateAttachment();

        var act = () => attachment.MarkClean([], "iv");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkClean_WhenEncryptionIvIsEmpty_ShouldThrowArgumentException(string value)
    {
        var attachment = CreateAttachment();

        var act = () => attachment.MarkClean(SampleEncryptedContent(), value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkInfected_ShouldSetStatusInfectedAndNoContent()
    {
        var attachment = CreateAttachment();

        attachment.MarkInfected();

        attachment.ScanStatus.Should().Be(FileScanStatus.Infected);
        attachment.ScanCompletedAt.Should().NotBeNull();
        attachment.EncryptedContent.Should().BeNull();
        attachment.EncryptionIv.Should().BeNull();
    }

    [Fact]
    public void MarkInfected_WhenAlreadyScanned_ShouldThrowInvalidOperationException()
    {
        var attachment = CreateAttachment();
        attachment.MarkInfected();

        var act = () => attachment.MarkInfected();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkScanFailed_ShouldSetStatusFailedAndNoContent()
    {
        var attachment = CreateAttachment();

        attachment.MarkScanFailed();

        attachment.ScanStatus.Should().Be(FileScanStatus.Failed);
        attachment.ScanCompletedAt.Should().NotBeNull();
        attachment.EncryptedContent.Should().BeNull();
        attachment.EncryptionIv.Should().BeNull();
    }

    [Fact]
    public void MarkScanFailed_WhenAlreadyScanned_ShouldThrowInvalidOperationException()
    {
        var attachment = CreateAttachment();
        attachment.MarkScanFailed();

        var act = () => attachment.MarkScanFailed();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsAvailable_WhenPending_ShouldReturnFalse()
    {
        var attachment = CreateAttachment();
        attachment.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_WhenClean_ShouldReturnTrue()
    {
        var attachment = CreateAttachment();
        attachment.MarkClean(SampleEncryptedContent(), "iv");
        attachment.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_WhenInfected_ShouldReturnFalse()
    {
        var attachment = CreateAttachment();
        attachment.MarkInfected();
        attachment.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_WhenScanFailed_ShouldReturnFalse()
    {
        var attachment = CreateAttachment();
        attachment.MarkScanFailed();
        attachment.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Delete_ShouldMarkAttachmentAsDeleted()
    {
        var attachment = CreateAttachment();

        attachment.Delete();

        attachment.IsDeleted.Should().BeTrue();
        attachment.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Delete_WhenCalledTwice_ShouldNotChangeDeletedAt()
    {
        var attachment = CreateAttachment();
        attachment.Delete();
        var firstDeletedAt = attachment.DeletedAt;

        attachment.Delete();

        attachment.DeletedAt.Should().Be(firstDeletedAt);
    }
}