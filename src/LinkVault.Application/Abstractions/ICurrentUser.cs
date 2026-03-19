namespace LinkVault.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsProTier { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}