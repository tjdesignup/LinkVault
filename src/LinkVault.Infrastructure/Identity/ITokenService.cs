using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LinkVault.Application.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace LinkVault.Infrastructure.Identity;

public sealed class TokenService(IVaultKeyProvider keyProvider, string issuer, string audience, int accessTokenMinutes = 15) : ITokenService
{
    private readonly IVaultKeyProvider _keyProvider = keyProvider;
    private readonly string _issuer = issuer;
    private readonly string _audience = audience;
    private readonly int _accessTokenMinutes = accessTokenMinutes;

    private string? _cachedJwtSecret;
    private readonly Lock _lock = new();

    public string GenerateAccessToken(Guid userId, bool isProTier, string role)
    {
        var jwtSecret = GetJwtSecret();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("tier", isProTier ? "Pro" : "Free"),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GetJwtSecret()
    {
        if (_cachedJwtSecret != null) return _cachedJwtSecret;

        lock (_lock)
        {
            _cachedJwtSecret ??= _keyProvider.GetJwtSecretAsync().GetAwaiter().GetResult();
            return _cachedJwtSecret;
        }
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string plainTextToken)
    {
        var bytes = Encoding.UTF8.GetBytes(plainTextToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}