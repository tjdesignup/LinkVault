namespace LinkVault.Application.Abstractions;

public interface IRedisLockService
{
    Task<bool> AcquireAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<IAsyncDisposable> AcquireWithRenewalAsync(
        string key,
        TimeSpan expiry,
        CancellationToken ct = default);

    Task<bool> IsLockedAsync(
        string key,
        CancellationToken cancellationToken = default);
}