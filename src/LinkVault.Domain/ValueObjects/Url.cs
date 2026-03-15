namespace LinkVault.Domain.ValueObjects;

public sealed record Url
{
    public string Value { get; }

    public Url(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("URL cannot be empty.", nameof(value));

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException($"'{value}' is not a valid URL. Only http and https are allowed.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;
}