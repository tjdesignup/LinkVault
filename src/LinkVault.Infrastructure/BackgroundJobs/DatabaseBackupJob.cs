using System.IO.Compression;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LinkVault.Infrastructure.BackgroundJobs;

public sealed class DatabaseBackupJob : BackgroundService
{
    private static readonly TimeOnly BackupTime = new(3, 0);

    private readonly string _connectionString;
    private readonly string _r2BucketName;
    private readonly ILogger<DatabaseBackupJob> _logger;
    private readonly AmazonS3Client _s3Client;

    public DatabaseBackupJob(
        string connectionString,
        string r2AccountId,
        string r2AccessKeyId,
        string r2SecretAccessKey,
        string r2BucketName,
        ILogger<DatabaseBackupJob> logger)
    {
        _connectionString = connectionString;
        _r2BucketName = r2BucketName;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{r2AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true 
        };

        _s3Client = new AmazonS3Client(r2AccessKeyId, r2SecretAccessKey, config);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextBackup();
            _logger.LogInformation("DatabaseBackupJob: next backup in {Delay}.", delay);

            await Task.Delay(delay, stoppingToken);
            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm");
        var dumpFileName = $"backup_{timestamp}.sql";
        var compressedFileName = $"{dumpFileName}.gz";
        var tempPath = Path.Combine(Path.GetTempPath(), compressedFileName);

        try
        {
            _logger.LogInformation("DatabaseBackupJob: starting backup {FileName}.", compressedFileName);

            await DumpAndCompressAsync(tempPath, ct);

            await UploadToR2Async(tempPath, compressedFileName, ct);

            _logger.LogInformation("DatabaseBackupJob: backup {FileName} completed.", compressedFileName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "DatabaseBackupJob: backup failed.");
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private async Task DumpAndCompressAsync(string outputPath, CancellationToken ct)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(_connectionString);

        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "pg_dump",
            Arguments = $"-h {builder.Host} -p {builder.Port} -U {builder.Username} -d {builder.Database} --no-password",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            Environment =
            {
                ["PGPASSWORD"] = builder.Password
            }
        };

        using var process = System.Diagnostics.Process.Start(processInfo)
            ?? throw new InvalidOperationException("Failed to start pg_dump process.");

        await using var fileStream = new FileStream(outputPath, FileMode.CreateNew);
        await using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);

        await process.StandardOutput.BaseStream.CopyToAsync(gzipStream, ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(ct);
            throw new InvalidOperationException($"pg_dump failed with exit code {process.ExitCode}: {error}");
        }
    }

    private async Task UploadToR2Async(string filePath, string objectKey, CancellationToken ct)
    {
        var transferUtility = new TransferUtility(_s3Client);

        var request = new TransferUtilityUploadRequest
        {
            FilePath = filePath,
            BucketName = _r2BucketName,
            Key = $"backups/{objectKey}",
            ContentType = "application/gzip"
        };

        await transferUtility.UploadAsync(request, ct);
    }

    private static TimeSpan CalculateDelayUntilNextBackup()
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.Add(BackupTime.ToTimeSpan());

        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        return nextRun - now;
    }
}