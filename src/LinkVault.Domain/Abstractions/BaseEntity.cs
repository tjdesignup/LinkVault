namespace LinkVault.Domain.Abstractions;

public abstract class BaseEntity : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public uint RowVersion { get; init; } 
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void SoftDelete()
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}