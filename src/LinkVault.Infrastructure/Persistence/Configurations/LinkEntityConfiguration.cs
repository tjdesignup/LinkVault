using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace LinkVault.Infrastructure.Persistence.Configurations;

public class LinkEntityConfiguration : BaseEntityConfiguration<LinkEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LinkEntity> builder)
    {
        builder.ToTable("links");

        builder.Property(l => l.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.HasIndex(l => l.UserId)
            .HasDatabaseName("ix_links_user_id");

        builder.Property(l => l.Url)
            .IsRequired()
            .HasMaxLength(2048)
            .HasColumnName("url")
            .HasConversion(
                url => url.Value,     
                value => new Url(value));   

        builder.Property(l => l.Title)
            .HasMaxLength(500)
            .HasColumnName("title");

        builder.Property(l => l.Note)
            .HasMaxLength(2000)
            .HasColumnName("note");

        builder.Property(l => l.OgTitle)
            .HasMaxLength(500)
            .HasColumnName("og_title");

        builder.Property(l => l.OgDescription)
            .HasMaxLength(2000)
            .HasColumnName("og_description");

        builder.Property(l => l.OgImageUrl)
            .HasMaxLength(2048)
            .HasColumnName("og_image_url");

        builder.Property(l => l.MetadataStatus)
            .IsRequired()
            .HasColumnName("metadata_status")
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<MetadataStatus>(value));

        builder.Property(l => l.Tags)
                    .IsRequired()
                    .HasColumnName("tags")
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

        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnName("search_vector")
            .IsRequired(false);

        builder.HasMany<FileAttachmentEntity>()
            .WithOne()
            .HasForeignKey(f => f.LinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}