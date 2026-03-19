using LinkVault.Application.Abstractions;

namespace LinkVault.Infrastructure.Storage;

public sealed class QuarantineStorageService : IQuarantineStorageService
{
    private readonly string _quarantinePath;

    public QuarantineStorageService(string quarantinePath)
    {
        _quarantinePath = quarantinePath;

        if (!Directory.Exists(_quarantinePath))
            Directory.CreateDirectory(_quarantinePath);
    }

    public async Task<byte[]> ReadAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        var path = BuildPath(storedFileName);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Quarantine file '{storedFileName}' not found.", path);

        return await File.ReadAllBytesAsync(path, cancellationToken);
    }

    public async Task SaveAsync(
        string storedFileName,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var path = BuildPath(storedFileName);

        await using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(fs, cancellationToken);
    }

    public Task DeleteAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        var path = BuildPath(storedFileName);

        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    private string BuildPath(string storedFileName)
        => Path.Combine(_quarantinePath, storedFileName);
}