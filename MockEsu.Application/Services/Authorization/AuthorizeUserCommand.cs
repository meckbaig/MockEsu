using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.DataBaseProvider;
using System.Security.Authentication;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Domain.Entities.Authentification;

namespace MockEsu.Application.Services.Authorization;

public record AuthorizeUserCommand : BaseRequest<AuthorizeUserResponse>
{
    public int userId { get; set; }
    public string password { get; set; }
    //public Dictionary<string, string> customClaims { get; set; }
}

public class AuthorizeUserResponse : BaseResponse
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
}

public class AuthorizeUserCommandValidator : AbstractValidator<AuthorizeUserCommand>
{
    public AuthorizeUserCommandValidator()
    {
        RuleFor(x => x.userId).GreaterThan(0);
        RuleFor(x => x.password).MinimumLength(6);
    }
}

public class AuthorizeUserCommandHandler : IRequestHandler<AuthorizeUserCommand, AuthorizeUserResponse>
{
    private readonly IAppDbContext _context;
    private readonly IJwtProvider _jwtProvider;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthorizeUserCommandHandler(
        IAppDbContext context, 
        IJwtProvider jwtProvider, 
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _jwtProvider = jwtProvider;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthorizeUserResponse> Handle(AuthorizeUserCommand request, CancellationToken cancellationToken)
    {
        User? user = _context.Users.Include(u => u.RefreshTokens).WithRoleById(request.userId);
        ValidateAuthorization(request, user);

        string jwt = _jwtProvider.GenerateToken(user);
        string refreshToken = _jwtProvider.GenerateRefreshToken(user);

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthorizeUserResponse { Token = jwt, RefreshToken = refreshToken };
    }

    private void ValidateAuthorization(AuthorizeUserCommand request, User user)
    {
        if (user == null)
            throw new Common.Exceptions.ValidationException(
                nameof(request.userId),
                [new ErrorItem($"Unable to find user with id {request.userId}", ValidationErrorCode.EntityIdValidator)]);
        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.password) == PasswordVerificationResult.Failed)
            throw new Common.Exceptions.ValidationException(
                nameof(request.password),
                [new ErrorItem($"Password is incorrect", ValidationErrorCode.PasswordIncorrectValidator)]);
    }
}