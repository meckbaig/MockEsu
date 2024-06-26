﻿using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities.Authentification;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MockEsu.Infrastructure.Authentification;

internal sealed class JwtProvider : IJwtProvider
{
    private static readonly TimeSpan TokenLifeTime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RefreshTokenLifeTime = TimeSpan.FromDays(3);
    private readonly JwtOptions _options;

    public JwtProvider(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public JwtProvider(JwtOptions options)
    {
        _options = options;
    }

    public TimeSpan GetRefreshTokenLifeTime() => RefreshTokenLifeTime;

    public string GenerateToken(
        User user, 
        TimeSpan? tokenLifeTime = null, 
        HashSet<Permission>? customPermissions = null)
    {
        var tokenHandler = new JsonWebTokenHandler();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(CustomClaim.UserId, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.Name),
        };
        foreach (var permission in customPermissions ?? user.Role.Permissions)
        {
            claims.Add(new("permissions", permission.Name));
        }

        var tokenDesctiptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(tokenLifeTime ?? TokenLifeTime),
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

    public int GetUserIdFromClaimsPrincipal(ClaimsPrincipal principal)
    {
        string? idString = principal.Claims.FirstOrDefault(c => c.Type == CustomClaim.UserId)?.Value;
        if (idString != null && int.TryParse(idString, out int id))
            return id;
        throw new ArgumentException("JWT key does not contain user id");
    }

    public string GenerateRefreshToken(User user, string? token = null)
    {
        var randomNumber = new byte[64];

        using (var generator = RandomNumberGenerator.Create())
        {
            generator.GetBytes(randomNumber);
        }

        string refreshToken = Convert.ToBase64String(randomNumber);

        if (token != null)
        {
            user.RefreshTokens
                .FirstOrDefault(t => t.Token.Equals(token))
                .Invalidated = true;
        }
        user.RefreshTokens
            .Add(new(refreshToken, DateTimeOffset.UtcNow.Add(RefreshTokenLifeTime)));


        return refreshToken;
    }
}

public static class CustomClaim
{
    public const string UserId = "userId";
}