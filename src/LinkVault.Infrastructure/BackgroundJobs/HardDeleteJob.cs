// src/LinkVault.Infrastructure/BackgroundJobs/HardDeleteJob.cs

using LinkVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LinkVault.Infrastructure.BackgroundJobs;

public sealed class HardDeleteJob(IServiceScopeFactory scopeFactory, ILogger<HardDeleteJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<HardDeleteJob> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cutoff = DateTime.UtcNow - RetentionPeriod;

            var deleted = await db.Links
                .IgnoreQueryFilters()
                .Where(l => l.IsDeleted && l.DeletedAt < cutoff)
                .ExecuteDeleteAsync(ct);

            if (deleted > 0)
                _logger.LogInformation("HardDeleteJob: permanently deleted {Count} links older than {Days} days.", deleted, RetentionPeriod.Days);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "HardDeleteJob failed.");
        }
    }
}