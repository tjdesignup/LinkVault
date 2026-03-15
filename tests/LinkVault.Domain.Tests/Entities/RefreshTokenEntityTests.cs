using FluentAssertions;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Entities;

namespace LinkVault.Domain.Tests.Entities;

public class RefreshTokenEntityTests
{
    private static RefreshTokenEntity CreateToken(
        Guid? userId = null,
        string tokenHash = "hash123",
        string deviceName = "Chrome / Windows",
        string ipAddress = "127.0.0.1")
        => RefreshTokenEntity.Create(userId ?? Guid.NewGuid(), tokenHash, deviceName, ipAddress);

    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        var userId = Guid.NewGuid();

        var token = RefreshTokenEntity.Create(userId, "hash123", "Chrome / Windows", "127.0.0.1");

        token.Id.Should().NotBe(Guid.Empty);
        token.UserId.Should().Be(userId);
        token.TokenHash.Should().Be("hash123");
        token.DeviceName.Should().Be("Chrome / Windows");
        token.IpAddress.Should().Be("127.0.0.1");
        token.IsRevoked.Should().BeFalse();
        token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Create_ShouldInheritFromBaseEntity()
    {
        var token = CreateToken();
        token.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void Create_WhenUserIdIsEmpty_ShouldThrowArgumentException()
    {
        var act = () => RefreshTokenEntity.Create(Guid.Empty, "hash", "device", "ip");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenTokenHashIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => RefreshTokenEntity.Create(Guid.NewGuid(), value, "device", "ip");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenDeviceNameIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => RefreshTokenEntity.Create(Guid.NewGuid(), "hash", value, "ip");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenIpAddressIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => RefreshTokenEntity.Create(Guid.NewGuid(), "hash", "device", value);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldExpireInSevenDays()
    {
        var before = DateTime.UtcNow.AddDays(7);

        var token = CreateToken();

        var after = DateTime.UtcNow.AddDays(7);
        token.ExpiresAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInFuture_ShouldReturnFalse()
    {
        var token = CreateToken();
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Revoke_ShouldSetIsRevokedTrue()
    {
        var token = CreateToken();

        token.Revoke();

        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Revoke_WhenCalledTwice_ShouldNotThrow()
    {
        var token = CreateToken();
        token.Revoke();

        var act = () => token.Revoke();

        act.Should().NotThrow();
    }
    
    [Fact]
    public void Replace_ShouldRevokeCurrentAndReturnNewToken()
    {
        var token = CreateToken();

        var newToken = token.Replace("new-hash", "Firefox / Linux", "192.168.1.1");

        token.IsRevoked.Should().BeTrue();
        newToken.TokenHash.Should().Be("new-hash");
        newToken.DeviceName.Should().Be("Firefox / Linux");
        newToken.IpAddress.Should().Be("192.168.1.1");
        newToken.UserId.Should().Be(token.UserId);
        newToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void Replace_WhenAlreadyRevoked_ShouldThrowInvalidOperationException()
    {
        var token = CreateToken();
        token.Revoke();

        var act = () => token.Replace("new-hash", "device", "ip");

        act.Should().Throw<InvalidOperationException>();
    }
}