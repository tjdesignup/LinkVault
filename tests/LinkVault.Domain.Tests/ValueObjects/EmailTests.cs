using FluentAssertions;
using LinkVault.Domain.ValueObjects;

namespace LinkVault.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("User@Example.COM")]
    [InlineData("user+tag@sub.domain.org")]
    public void Email_WhenValidFormat_ShouldCreate(string input)
    {
        var act = () => new Email(input);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("notanemail")]
    [InlineData("@nodomain.com")]
    public void Email_WhenInvalidFormat_ShouldThrowArgumentException(string input)
    {
        var act = () => new Email(input);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Email_ShouldNormalizeToLowercase()
    {
        var email = new Email("User@Example.COM");
        email.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Email_ShouldTrimWhitespace()
    {
        var email = new Email("  user@example.com  ");
        email.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Email_WhenSameValue_ShouldBeEqual()
    {
        var a = new Email("user@example.com");
        var b = new Email("USER@EXAMPLE.COM");
        a.Should().Be(b);
    }

    [Fact]
    public void Email_WhenDifferentValue_ShouldNotBeEqual()
    {
        var a = new Email("a@example.com");
        var b = new Email("b@example.com");
        a.Should().NotBe(b);
    }
}