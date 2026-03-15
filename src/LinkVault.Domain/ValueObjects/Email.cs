using System.Text.RegularExpressions;

namespace LinkVault.Domain.ValueObjects;

public partial record Email
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public string Value { get; }

    public Email(string value)
    {
        var cleanedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(cleanedValue) || !EmailRegex().IsMatch(cleanedValue))
        {
            throw new ArgumentException("Invalid e-mail format.", nameof(value));
        }

        Value = cleanedValue.ToLowerInvariant();
    }

    public override string ToString() => Value;
}