using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkVault.Infrastructure.Persistence.Configurations;

public class CurrentSubscriptionEntityConfiguration : BaseEntityConfiguration<CurrentSubscriptionEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CurrentSubscriptionEntity> builder)
    {
        builder.ToTable("current_subscriptions");

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.HasIndex(s => s.UserId)
            .IsUnique()
            .HasDatabaseName("ix_current_subscriptions_user_id");

        builder.Property(s => s.StripeCustomerId)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("stripe_customer_id");

        builder.HasIndex(s => s.StripeCustomerId)
            .IsUnique()
            .HasDatabaseName("ix_current_subscriptions_stripe_customer_id");

        builder.Property(s => s.StripeSubscriptionId)
            .HasMaxLength(100)
            .HasColumnName("stripe_subscription_id");

        builder.Property(s => s.Tier)
            .IsRequired()
            .HasColumnName("tier")
            .HasConversion(
                tier => tier.ToString(),
                value => Enum.Parse<SubscriptionTier>(value));

        builder.Property(s => s.Status)
            .IsRequired()
            .HasColumnName("status")
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<SubscriptionStatus>(value));

        builder.Property(s => s.CurrentPeriodEnd)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("current_period_end");
    }
}