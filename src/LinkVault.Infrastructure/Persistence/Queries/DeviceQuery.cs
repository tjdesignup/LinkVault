// src/LinkVault.Infrastructure/Persistence/Queries/DeviceQueries.cs

using LinkVault.Application.Abstractions.IQueries;
using LinkVault.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Infrastructure.Persistence.Queries;

public sealed class DeviceQueries(AppDbContext db) : IDeviceQueries
{
    private readonly AppDbContext _db = db;

    public async Task<List<DeviceDto>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        return await _db.RefreshTokens
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                t.IsRevoked == false &&
                t.IsExpired == false)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new DeviceDto(
                t.Id,
                t.DeviceName,
                t.IpAddress,
                t.CreatedAt,
                t.ExpiresAt
            ))
            .ToListAsync(ct);
    }
}