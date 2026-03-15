using LinkVault.Domain.Enums;

namespace LinkVault.Application.Abstractions;

public interface IVirusScanService
{
    Task<FileScanStatus> ScanAsync(
        string storedFileName,
        CancellationToken cancellationToken = default);
}