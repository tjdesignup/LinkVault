using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LinkVault.Domain.ValueObjects;

public partial record Slug
{
    [GeneratedRegex(@"^[a-z0-9\-]+$",RegexOptions.Compiled)]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"-{2,}",RegexOptions.Compiled)]
    private static partial Regex MultipleHyphens();

    public string Value { get; }

    public Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Slug cannot be empty.", nameof(value));

        if (value.Length > 100)
            throw new ArgumentException("Slug cannot exceed 100 characters.", nameof(value));

        if (!SlugRegex().IsMatch(value))
            throw new ArgumentException($"'{value}' is not a valid slug.", nameof(value));

        Value = value;
    }

    public static Slug Generate(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var slug = sb.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant()
            .Replace(' ', '-');

        slug = Regex.Replace(slug, @"[^a-z0-9\-]", string.Empty);

        slug = MultipleHyphens().Replace(slug, "-").Trim('-');

        if (slug.Length > 100)
            slug = slug[..100].TrimEnd('-');

        return new Slug(slug);
    }

    public override string ToString() => Value;
}