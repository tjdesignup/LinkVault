using System.Text.RegularExpressions;

namespace LinkVault.Domain.ValueObjects;

public partial record Tag
{
     [GeneratedRegex(@"^[a-z0-9\-]+$", RegexOptions.Compiled)]
    private static partial Regex TagRegex();

    public string Value { get; }

    public Tag(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("Tag cannot be empty.", nameof(value));

        if (normalized.Length > 50)
            throw new ArgumentException("Tag cannot exceed 50 characters.", nameof(value));

        if (!TagRegex().IsMatch(normalized))
            throw new ArgumentException($"'{value}' contains invalid characters. Only alphanumeric characters and hyphens are allowed.", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}