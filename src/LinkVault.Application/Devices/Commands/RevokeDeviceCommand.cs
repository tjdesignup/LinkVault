using LinkVault.Application.DTOs;
using MediatR;

namespace LinkVault.Application.Devices.Commands;

public record RevokeDeviceCommand(
    Guid DeviceId
) : IRequest<MessageDto>;