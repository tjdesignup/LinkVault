using LinkVault.Application.Abstractions;
using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Devices.Queries;

public class GetDevicesHandler(
    IDeviceQueries deviceQueries,
    ICurrentUser currentUser)
    : IRequestHandler<GetDevicesQuery, List<DeviceDto>>
{
    public async Task<List<DeviceDto>> Handle(
        GetDevicesQuery query,
        CancellationToken cancellationToken)
        => await deviceQueries.GetActiveByUserIdAsync(
            currentUser.UserId,
            cancellationToken);
}