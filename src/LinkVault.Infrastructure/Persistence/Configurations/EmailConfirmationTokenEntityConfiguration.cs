using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkVault.Infrastructure.Persistence.Configurations;

public class EmailConfirmationTokenEntityConfiguration : BaseEntityConfiguration<EmailConfirmationTokenEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<EmailConfirmationTokenEntity> builder)
    {
        builder.ToTable("email_confirmation_tokens");

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("token");

        builder.Property(e => e.IsUsed)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_used");

        builder.HasIndex(e => e.Token)
            .IsUnique()
            .HasDatabaseName("ix_email_confirmation_tokens_token");

        builder.Property(e => e.Type)
            .IsRequired()
            .HasColumnName("type")
            .HasConversion(
                type => type.ToString(),
                value => Enum.Parse<ConfirmationTokenType>(value));

        builder.Property(e => e.ExpiresAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasColumnName("expires_at");
    }
}