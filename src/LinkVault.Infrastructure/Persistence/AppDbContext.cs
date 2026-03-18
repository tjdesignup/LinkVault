using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<LinkEntity> Links => Set<LinkEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<EmailConfirmationTokenEntity> EmailConfirmationTokens => Set<EmailConfirmationTokenEntity>();
    public DbSet<CollectionEntity> Collections => Set<CollectionEntity>();
    public DbSet<FileAttachmentEntity> FileAttachments => Set<FileAttachmentEntity>();
    public DbSet<CurrentSubscriptionEntity> CurrentSubscriptions => Set<CurrentSubscriptionEntity>();
    public DbSet<SubscriptionEventEntity> SubscriptionEvents => Set<SubscriptionEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}