namespace LinkVault.Application.Abstractions;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, bool isProTier, string role);
    string GenerateRefreshToken();
    string HashRefreshToken(string plainTextToken);
}