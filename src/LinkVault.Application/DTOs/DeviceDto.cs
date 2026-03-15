namespace LinkVault.Application.DTOs;

public record DeviceDto(
    Guid Id,
    string DeviceName,
    string IpAddress,
    DateTime CreatedAt,
    DateTime ExpiresAt
);