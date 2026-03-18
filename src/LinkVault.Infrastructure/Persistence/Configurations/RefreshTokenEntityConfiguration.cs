using LinkVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkVault.Infrastructure.Persistence.Configurations;

public class RefreshTokenEntityConfiguration : BaseEntityConfiguration<RefreshTokenEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.Property(r => r.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(r => r.TokenHash)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("token_hash");

        builder.HasIndex(r => r.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_refresh_tokens_token_hash");

        builder.Property(r => r.DeviceName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("device_name");

        builder.Property(r => r.IpAddress)
            .IsRequired()
            .HasMaxLength(45) 
            .HasColumnName("ip_address");

        builder.Property(r => r.ExpiresAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasColumnName("expires_at");

        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("ix_refresh_tokens_user_id");
    }
}