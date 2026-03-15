using LinkVault.Domain.Abstractions;

namespace LinkVault.Domain.Entities;

public class RefreshTokenEntity : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    private RefreshTokenEntity() { }

    private RefreshTokenEntity(Guid userId, string tokenHash, string deviceName, string ipAddress)
    {
        UserId = userId;
        TokenHash = tokenHash;
        DeviceName = deviceName;
        IpAddress = ipAddress;
        ExpiresAt = DateTime.UtcNow.AddDays(7);
    }

    public static RefreshTokenEntity Create(Guid userId, string tokenHash, string deviceName, string ipAddress)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty.", nameof(tokenHash));

        if (string.IsNullOrWhiteSpace(deviceName))
            throw new ArgumentException("Device name cannot be empty.", nameof(deviceName));

        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be empty.", nameof(ipAddress));

        return new RefreshTokenEntity(userId, tokenHash, deviceName, ipAddress);
    }

    public void Revoke()
    {
        IsRevoked = true;
    }

    public RefreshTokenEntity Replace(string newTokenHash, string deviceName, string ipAddress)
    {
        if (IsRevoked)
            throw new InvalidOperationException("Cannot replace an already revoked token.");

        Revoke();

        return Create(UserId, newTokenHash, deviceName, ipAddress);
    }
}