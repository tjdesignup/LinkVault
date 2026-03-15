using LinkVault.Domain.ValueObjects;
using FluentAssertions;

namespace LinkVault.Domain.Tests.ValueObjects;

public class UrlTests
{
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("https://sub.domain.org/path?query=1#anchor")]
    public void Url_WhenValidHttpOrHttps_ShouldCreate(string input)
    {
        var url = new Url(input);
        
        url.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://example.com")]
    [InlineData("notaurl")]
    [InlineData("//example.com")]
    public void Url_WhenInvalidOrNonHttpScheme_ShouldThrowArgumentException(string input)
    {
        FluentActions.Invoking(()=> new Url(input))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Url_ShouldPreserveOriginalValue()
    {
        var input = "https://example.com/Path?Query=1";
        var url = new Url(input);
        input.Should().BeEquivalentTo(url.Value);
    }

    [Fact]
    public void Url_WhenSameValue_ShouldBeEqual()
    {
        var a = new Url("https://example.com");
        var b = new Url("https://example.com");
        a.Should().BeEquivalentTo(b);
    }
}