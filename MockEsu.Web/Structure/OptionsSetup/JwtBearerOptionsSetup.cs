﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Infrastructure.Authentification;
using System.Diagnostics;
using System.Text;

namespace MockEsu.Web.Structure.OptionsSetup;

public class JwtBearerOptionsSetup : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly JwtOptions _jwtOptions;
    private readonly IJwtProvider _jwtProvider;

    public JwtBearerOptionsSetup(IOptions<JwtOptions> jwtOptions, IJwtProvider jwtProvider)
    {
        _jwtOptions = jwtOptions.Value;
        _jwtProvider = jwtProvider;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
            ClockSkew = /*Debugger.IsAttached ? TimeSpan.Zero :*/ _jwtProvider.GetRefreshTokenLifeTime()
        };
    }
}
