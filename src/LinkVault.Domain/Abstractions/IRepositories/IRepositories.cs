namespace LinkVault.Domain.Abstractions.IRepositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}