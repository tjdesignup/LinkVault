using LinkVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkVault.Infrastructure.Persistence.Configurations;

public class UserEntityConfiguration : BaseEntityConfiguration<UserEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");

        builder.Property(u => u.EmailEncrypted)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("email_encrypted");

        builder.Property(u => u.EmailBlindIndexHash)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("email_blind_index_hash");

        builder.HasIndex(u => u.EmailBlindIndexHash)
            .IsUnique()
            .HasDatabaseName("ix_users_email_blind_index_hash");

        builder.Property(u => u.PendingEmailEncrypted)
            .HasMaxLength(500)
            .HasColumnName("pending_email_encrypted");

        builder.Property(u => u.PendingEmailBlindIndexHash)
            .HasMaxLength(100)
            .HasColumnName("pending_email_blind_index_hash");

        builder.HasIndex(u => u.PendingEmailBlindIndexHash)
            .IsUnique()
            .HasDatabaseName("ix_users_pending_email_blind_index_hash")
            .HasFilter("pending_email_blind_index_hash IS NOT NULL");

        builder.Property(u => u.FirstNameEncrypted)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("first_name_encrypted");

        builder.Property(u => u.SurNameEncrypted)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("surname_encrypted");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("password_hash");

        builder.Property(u => u.EncryptedDek)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("encrypted_dek");

        builder.Property(u => u.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("email_confirmed");

        builder.Property(u => u.IsAdmin)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_admin");

        builder.HasMany<RefreshTokenEntity>()
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<EmailConfirmationTokenEntity>()
            .WithOne()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<LinkEntity>()
            .WithOne()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<CollectionEntity>()
            .WithOne()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<FileAttachmentEntity>()
            .WithOne()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<SubscriptionEventEntity>()
            .WithOne()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CurrentSubscriptionEntity>()
            .WithOne()
            .HasForeignKey<CurrentSubscriptionEntity>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}