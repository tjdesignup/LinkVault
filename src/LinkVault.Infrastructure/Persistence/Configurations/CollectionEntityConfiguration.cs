using LinkVault.Domain.Entities;
using LinkVault.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkVault.Infrastructure.Persistence.Configurations;

public class CollectionEntityConfiguration : BaseEntityConfiguration<CollectionEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CollectionEntity> builder)
    {
        builder.ToTable("collections");

        builder.Property(c => c.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("slug")
            .HasConversion(
                slug => slug.Value,
                value => new Slug(value));

        builder.HasIndex(c => new { c.UserId, c.Slug })
            .IsUnique()
            .HasDatabaseName("ix_collections_user_id_slug");

        builder.Property(c => c.FilterTags)
            .IsRequired()
            .HasColumnName("filter_tags")
            .HasColumnType("jsonb")
            .HasConversion(
                tags => System.Text.Json.JsonSerializer.Serialize(
                    tags.Select(t => t.Value).ToList(),
                    (System.Text.Json.JsonSerializerOptions?)null),
                value => System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                    value,
                    (System.Text.Json.JsonSerializerOptions?)null)!
                        .Select(t => new Tag(t))
                        .ToList());

        builder.Property(c => c.IsPublic)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_public");

        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("ix_collections_user_id");
    }
}