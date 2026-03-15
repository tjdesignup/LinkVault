namespace LinkVault.Application.Abstractions;

public interface IMasterKeyProvider
{
    Task<string> GetMasterKeyAsync(CancellationToken cancellationToken = default);
}