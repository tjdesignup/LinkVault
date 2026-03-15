using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Devices.Queries;

public record GetDevicesQuery : IRequest<List<DeviceDto>>;