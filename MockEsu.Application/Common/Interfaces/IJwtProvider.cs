using MockEsu.Domain.Entities.Authentification;
using System.Security.Claims;

namespace MockEsu.Application.Common.Interfaces;

public interface IJwtProvider
{
    TimeSpan GetRefreshTokenLifeTime();
    string GenerateToken(User user);
    string GenerateRefreshToken(User user, string? token = null);
    int GetUserIdFromClaimsPrincipal(ClaimsPrincipal principal);
}
