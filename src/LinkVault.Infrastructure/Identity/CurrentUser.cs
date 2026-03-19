using System.Security.Claims;
using LinkVault.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace LinkVault.Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            var claim = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("UserId claim not found.");
            return Guid.Parse(claim);
        }
    }

    public bool IsProTier =>
        User?.FindFirstValue("tier") == "Pro";

    public bool IsAdmin =>
        User?.FindFirstValue(ClaimTypes.Role) == "Admin";
}