using FluentAssertions;
using LinkVault.Domain.ValueObjects;

namespace LinkVault.Domain.Tests.ValueObjects;

public class SlugTests
{
    [Theory]
    [InlineData("my-collection")]
    [InlineData("dotnet-tips-2024")]
    [InlineData("a")]
    public void Slug_WhenValid_ShouldCreate(string input)
    {
        var slug = new Slug(input);

        slug.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Has Spaces")]
    [InlineData("has_underscore")]
    [InlineData("has!special")]
    public void Slug_WhenInvalid_ShouldThrowArgumentException(string input)
    {
        FluentActions.Invoking(()=>new Slug(input))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Slug_WhenTooLong_ShouldThrowArgumentException()
    {
        var longSlug = new string('a', 101);
        FluentActions.Invoking(()=>new Slug(longSlug))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Slug_ShouldPreserveValue()
    {
        var slug = new Slug("my-collection");

        slug.Value.Should().BeEquivalentTo("my-collection");
    }

    [Fact]
    public void Slug_WhenSameValue_ShouldBeEqual()
    {
        var a = new Slug("my-collection");
        var b = new Slug("my-collection");

        a.Should().BeEquivalentTo(b);
    }

    [Fact]
    public void Slug_Generate_WhenSimpleText_ShouldCreateValidSlug()
    {
        var slug = Slug.Generate("My Collection");

        slug.Value.Should().BeEquivalentTo("my-collection");
    }

    [Fact]
    public void Slug_Generate_WhenTextWithDiacritics_ShouldRemoveDiacritics()
    {
        var slug = Slug.Generate("Česká kolekce");

        slug.Value.Should().NotContain("č");
        slug.Value.Should().NotContain("á");
    }

    [Fact]
    public void Slug_Generate_WhenTextTooLong_ShouldTruncateTo100Chars()
    {
        var longText = new string('a', 150);
        var slug = Slug.Generate(longText);

        slug.Value.Length.Should().BeLessThanOrEqualTo(100);
    }
}