using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities;
using System.Security.Claims;
using System.Text;

namespace MockEsu.Infrastructure.Authentification;

internal sealed class JwtProvider : IJwtProvider
{
    private static readonly TimeSpan TokenLifeTime = TimeSpan.FromHours(1);
    private readonly JwtOptions _options;

    public JwtProvider(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JsonWebTokenHandler();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Name, user.Name),
            //new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.Name),
        };
        foreach (var permission in user.Role.Permissions)
        {
            claims.Add(new("permissions", permission));
        }

        var tokenDesctiptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TokenLifeTime),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
                SecurityAlgorithms.HmacSha256)
        };
        var token = tokenHandler.CreateToken(tokenDesctiptor);
        var jwt = tokenHandler.ReadJsonWebToken(token);
        return jwt.UnsafeToString();
    }
}