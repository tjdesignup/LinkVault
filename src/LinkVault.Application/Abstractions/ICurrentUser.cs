namespace LinkVault.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
    string IndexEmailHash { get; }
    bool IsProTier { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}