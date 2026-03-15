using LinkVault.Application.DTOs;

namespace LinkVault.Application.Abstractions.IQueries;

public interface IDeviceQueries
{
    Task<List<DeviceDto>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}