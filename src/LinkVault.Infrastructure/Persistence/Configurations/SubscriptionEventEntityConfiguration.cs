using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkVault.Infrastructure.Persistence.Configurations;

public class SubscriptionEventEntityConfiguration : BaseEntityConfiguration<SubscriptionEventEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SubscriptionEventEntity> builder)
    {
        builder.ToTable("subscription_events");

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(e => e.StripeSubscriptionId)
            .HasMaxLength(100)
            .HasColumnName("stripe_subscription_id");

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasColumnName("event_type")
            .HasConversion(
                type => type.ToString(),
                value => Enum.Parse<SubscriptionEventType>(value));

        builder.Property(e => e.Tier)
            .IsRequired()
            .HasColumnName("tier")
            .HasConversion(
                tier => tier.ToString(),
                value => Enum.Parse<SubscriptionTier>(value));

        builder.Property(e => e.Status)
            .IsRequired()
            .HasColumnName("status")
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<SubscriptionStatus>(value));

        builder.Property(e => e.CurrentPeriodEnd)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("current_period_end");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_subscription_events_user_id");
    }
}