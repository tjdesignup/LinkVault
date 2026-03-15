using FluentAssertions;
using LinkVault.Domain.ValueObjects;

namespace LinkVault.Domain.Tests.ValueObjects;

public class TagTests
{
    [Theory]
    [InlineData("dotnet")]
    [InlineData("c-sharp")]
    [InlineData("web123")]
    [InlineData("A")]
    public void Tag_WhenValid_ShouldCreate(string input)
    {
        var tag = new Tag(input);
        tag.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid tag")]    
    [InlineData("invalid_tag")]      
    [InlineData("invalid!tag")]    
    public void Tag_WhenInvalid_ShouldThrowArgumentException(string input)
    {
        FluentActions.Invoking(()=> new Tag(input))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Tag_WhenTooLong_ShouldThrowArgumentException()
    {
        var longTag = new string('a', 51);
        
        FluentActions.Invoking(()=> new Tag(longTag))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Tag_ShouldNormalizeToLowercase()
    {
        var tag = new Tag("DotNet");
        tag.Value.Should().BeEquivalentTo("dotnet");
    }

    [Fact]
    public void Tag_ShouldTrimWhitespace()
    {
        var tag = new Tag("  dotnet  ");
        tag.Value.Should().BeEquivalentTo("dotnet");
    }

    [Fact]
    public void Tag_WhenSameValue_ShouldBeEqual()
    {
        var a = new Tag("dotnet");
        var b = new Tag("DOTNET");
        a.Should().BeEquivalentTo(b);
    }
}