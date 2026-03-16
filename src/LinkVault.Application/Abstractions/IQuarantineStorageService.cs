namespace LinkVault.Application.Abstractions;

public interface IQuarantineStorageService
{
    Task<byte[]> ReadAsync(
        string storedFileName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storedFileName,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        string storedFileName,
        Stream fileStream,
        CancellationToken cancellationToken = default);
}