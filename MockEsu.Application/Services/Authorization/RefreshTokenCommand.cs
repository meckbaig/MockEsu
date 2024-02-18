﻿using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.DataBaseProvider;
using MockEsu.Domain.Entities.Authentification;
using System.Security.Claims;

namespace MockEsu.Application.Services.Authorization;

public record RefreshTokenCommand : BaseRequest<RefreshTokenResponse>
{
    public string refreshToken { get; set; }
    public ClaimsPrincipal principal { get; set; }
}

public class RefreshTokenResponse : BaseResponse
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IAppDbContext _context;
    private readonly IJwtProvider _jwtProvider;

    public RefreshTokenCommandHandler(IAppDbContext context, IJwtProvider jwtProvider)
    {
        _context = context;
        _jwtProvider = jwtProvider;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        int userId = _jwtProvider.GetUserIdFromClaimsPrincipal(request.principal);
        User user = _context.Users.Include(u => u.RefreshToken).WithRoleById(userId);
        if (user == null)
        {
            throw new Common.Exceptions.ValidationException(
                "JWT token",
                [new ErrorItem($"Unable to find user with id {userId}.", ValidationErrorCode.EntityIdValidator)]);
        }
        if (!user.RefreshToken.Token.Equals(request.refreshToken))
        {
            throw new Common.Exceptions.ValidationException(
                nameof(request.refreshToken),
                [new ErrorItem($"Refresh token is not valid.", ValidationErrorCode.RefreshTokenNotValid)]);
        }
        if (user.RefreshToken.ExpirationDate < DateTimeOffset.UtcNow)
        {
            throw new Common.Exceptions.ValidationException(
                nameof(request.refreshToken),
                [new ErrorItem($"Refresh token has expired.", ValidationErrorCode.RefreshTokenHasExpired)]);
        }

        string jwt = _jwtProvider.GenerateToken(user);
        string refreshToken = _jwtProvider.GenerateRefreshToken(user);

        await _context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse { Token = jwt, RefreshToken = refreshToken };
    }
}