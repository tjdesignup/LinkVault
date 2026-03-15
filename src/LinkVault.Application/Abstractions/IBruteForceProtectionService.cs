namespace LinkVault.Application.Abstractions;

public interface IBruteForceProtectionService
{
    Task<bool> IsLockedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TimeSpan> GetRemainingLockTimeAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> RecordFailedAttemptAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task ResetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}