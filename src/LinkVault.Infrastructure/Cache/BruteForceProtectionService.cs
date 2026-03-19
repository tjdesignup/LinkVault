using LinkVault.Application.Abstractions;
using StackExchange.Redis;

namespace LinkVault.Infrastructure.Cache;

public sealed class BruteForceProtectionService(IConnectionMultiplexer redis) : IBruteForceProtectionService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

    private readonly IDatabase _db = redis.GetDatabase();

    private static string CounterKey(Guid userId) => $"brute:counter:{userId}";
    private static string LockKey(Guid userId) => $"brute:lock:{userId}";

    public async Task<bool> IsLockedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.KeyExistsAsync(LockKey(userId));
    }

    public async Task<TimeSpan> GetRemainingLockTimeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ttl = await _db.KeyTimeToLiveAsync(LockKey(userId));
        return ttl ?? TimeSpan.Zero;
    }

    public async Task<bool> RecordFailedAttemptAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var key = CounterKey(userId);

        var count = await _db.StringIncrementAsync(key);

        if (count == 1)
            await _db.KeyExpireAsync(key, LockDuration);

        if (count >= MaxFailedAttempts)
        {
            await _db.StringSetAsync(LockKey(userId), "1", LockDuration);
            await _db.KeyDeleteAsync(key);
            return true;
        }

        return false;
    }

    public async Task ResetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await _db.KeyDeleteAsync(CounterKey(userId));
        await _db.KeyDeleteAsync(LockKey(userId));
    }
}