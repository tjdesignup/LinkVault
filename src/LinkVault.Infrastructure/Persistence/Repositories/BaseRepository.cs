using LinkVault.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence.Repositories;

public abstract class BaseRepository<TEntity>(AppDbContext context)
    where TEntity : BaseEntity
{
    protected readonly AppDbContext Context = context;

    public async Task<TEntity?> FindByIdAsync(
        Guid id,
        CancellationToken ct = default)
        => await Context.Set<TEntity>()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddAsync(
        TEntity entity,
        CancellationToken ct = default)
        => await Context.Set<TEntity>().AddAsync(entity, ct);

    public Task DeleteAsync(
        TEntity entity,
        CancellationToken ct = default)
    {
        entity.SoftDelete();
        return Task.CompletedTask;
    }
}