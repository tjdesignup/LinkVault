using FluentAssertions;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;

namespace LinkVault.Domain.Tests.Entities;

public class EmailConfirmationTokenEntityTests
{
    private static EmailConfirmationTokenEntity CreateToken(
        Guid? userId = null,
        string token = "secure-token-123",
        ConfirmationTokenType type = ConfirmationTokenType.Registration)
        => EmailConfirmationTokenEntity.Create(userId ?? Guid.NewGuid(), token, type);

    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        var userId = Guid.NewGuid();

        var confirmationToken = EmailConfirmationTokenEntity.Create(
            userId, "secure-token-123", ConfirmationTokenType.Registration);

        confirmationToken.Id.Should().NotBe(Guid.Empty);
        confirmationToken.UserId.Should().Be(userId);
        confirmationToken.Token.Should().Be("secure-token-123");
        confirmationToken.Type.Should().Be(ConfirmationTokenType.Registration);
        confirmationToken.IsUsed.Should().BeFalse();
        confirmationToken.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
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
        var act = () => EmailConfirmationTokenEntity.Create(
            Guid.Empty, "token", ConfirmationTokenType.Registration);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenTokenIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => EmailConfirmationTokenEntity.Create(
            Guid.NewGuid(), value, ConfirmationTokenType.Registration);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmailChangeType_ShouldSetTypeCorrectly()
    {
        var token = EmailConfirmationTokenEntity.Create(
            Guid.NewGuid(), "token", ConfirmationTokenType.EmailChange);

        token.Type.Should().Be(ConfirmationTokenType.EmailChange);
    }

    [Fact]
    public void Create_ShouldExpireIn24Hours()
    {
        var before = DateTime.UtcNow.AddHours(24);

        var token = CreateToken();

        var after = DateTime.UtcNow.AddHours(24);
        token.ExpiresAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInFuture_ShouldReturnFalse()
    {
        var token = CreateToken();
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Use_WhenValidToken_ShouldMarkAsUsed()
    {
        var token = CreateToken();

        token.Use();

        token.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void Use_WhenAlreadyUsed_ShouldThrowInvalidConfirmationTokenException()
    {
        var token = CreateToken();
        token.Use();

        var act = () => token.Use();

        act.Should().Throw<InvalidConfirmationTokenException>();
    }

    [Fact]
    public void Use_WhenExpired_ShouldThrowInvalidConfirmationTokenException()
    {
        var token = CreateToken();

        typeof(EmailConfirmationTokenEntity)
            .GetProperty(nameof(EmailConfirmationTokenEntity.ExpiresAt))!
            .SetValue(token, DateTime.UtcNow.AddHours(-1));

        var act = () => token.Use();

        act.Should().Throw<ConfirmationTokenExpiredException>();
    }
}