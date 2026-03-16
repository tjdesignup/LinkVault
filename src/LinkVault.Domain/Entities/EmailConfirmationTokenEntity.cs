using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Enums;
using LinkVault.Domain.Exceptions;

namespace LinkVault.Domain.Entities;

public class EmailConfirmationTokenEntity : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public ConfirmationTokenType Type { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    private EmailConfirmationTokenEntity() { }

    private EmailConfirmationTokenEntity(Guid userId, string token, ConfirmationTokenType type)
    {
        UserId = userId;
        Token = token;
        Type = type;
        ExpiresAt = DateTime.UtcNow.AddHours(24);
    }

    public static EmailConfirmationTokenEntity Create(Guid userId, string token, ConfirmationTokenType type)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.", nameof(token));

        return new EmailConfirmationTokenEntity(userId, token, type);
    }

    public void Use()
    {
        if (IsUsed)
            throw new InvalidConfirmationTokenException();

        if (IsExpired)
            throw new ConfirmationTokenExpiredException();

        IsUsed = true;
    }
}