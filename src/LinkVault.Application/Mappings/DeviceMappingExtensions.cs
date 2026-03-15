using LinkVault.Application.DTOs;
using LinkVault.Domain.Entities;

namespace LinkVault.Application.Mappings;

public static class DeviceMappingExtensions
{
    public static DeviceDto ToDto(this RefreshTokenEntity token)
        => new(
            Id: token.Id,
            DeviceName: token.DeviceName,
            IpAddress: token.IpAddress,
            CreatedAt: token.CreatedAt,
            ExpiresAt: token.ExpiresAt
        );
}