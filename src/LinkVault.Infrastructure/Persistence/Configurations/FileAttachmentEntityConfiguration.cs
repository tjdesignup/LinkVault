using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkVault.Infrastructure.Persistence.Configurations;

public class FileAttachmentEntityConfiguration : BaseEntityConfiguration<FileAttachmentEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<FileAttachmentEntity> builder)
    {
        builder.ToTable("file_attachments");

        builder.Property(f => f.LinkId)
            .IsRequired()
            .HasColumnName("link_id");

        builder.Property(f => f.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(f => f.OriginalFileName)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("original_file_name");

        builder.Property(f => f.StoredFileName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("stored_file_name");

        builder.Property(f => f.MimeType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("mime_type");

        builder.Property(f => f.FileSizeBytes)
            .IsRequired()
            .HasColumnName("file_size_bytes");

        builder.Property(f => f.ScanStatus)
            .IsRequired()
            .HasColumnName("scan_status")
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<FileScanStatus>(value));

        builder.Property(f => f.ScanCompletedAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("scan_completed_at");
            
        builder.Property(f => f.EncryptedContent)
            .HasColumnName("encrypted_content")
            .HasColumnType("bytea");

        builder.Property(f => f.EncryptionIv)
            .HasMaxLength(100)
            .HasColumnName("encryption_iv");

        builder.HasIndex(f => f.LinkId)
            .HasDatabaseName("ix_file_attachments_link_id");

        builder.HasIndex(f => f.UserId)
            .HasDatabaseName("ix_file_attachments_user_id");
    }
}