using LinkVault.Application.Abstractions;
using StackExchange.Redis;

namespace LinkVault.Infrastructure.Cache;

public sealed class RedisLockService(IConnectionMultiplexer redis) : IRedisLockService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<bool> AcquireAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        return await _db.StringSetAsync(
            key,
            "1",
            expiry,
            When.NotExists);
    }

    public async Task ReleaseAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> IsLockedAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task<IAsyncDisposable> AcquireWithRenewalAsync(
        string key,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var acquired = await AcquireAsync(key, expiry, ct);

        if (!acquired)
            throw new InvalidOperationException($"Failed to acquire lock for key '{key}'.");

        return new RedisLockHandle(this, key, expiry);
    }

    public async Task ExtendAsync(string key, TimeSpan expiry)
    {
        await _db.KeyExpireAsync(key, expiry);
    }

    private sealed class RedisLockHandle : IAsyncDisposable
    {
        private readonly RedisLockService _lockService;
        private readonly string _key;
        private readonly TimeSpan _expiry;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _renewalTask;
        private bool _disposed;

        public RedisLockHandle(RedisLockService lockService, string key, TimeSpan expiry)
        {
            _lockService = lockService;
            _key = key;
            _expiry = expiry;
            _renewalTask = RenewLoopAsync(_cts.Token);
        }

        private async Task RenewLoopAsync(CancellationToken ct)
        {
            var interval = TimeSpan.FromTicks(_expiry.Ticks / 2);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(interval, ct);
                    await _lockService.ExtendAsync(_key, _expiry);
                }
            }
            catch (OperationCanceledException){}
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();
            _cts.Dispose();

            try { await _renewalTask; } catch {}

            await _lockService.ReleaseAsync(_key);
        }
    }
}