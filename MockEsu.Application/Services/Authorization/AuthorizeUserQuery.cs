using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.DataBaseProvider;
using MockEsu.Domain.Entities;
using System.Security.Authentication;
using MockEsu.Application.Common.Exceptions;

namespace MockEsu.Application.Services.Authorization;

public record AuthorizeUserQuery : BaseRequest<AuthorizeUserResponse>
{
    public int userId { get; set; }
    public string password { get; set; }
    //public Dictionary<string, string> customClaims { get; set; }
}

public class AuthorizeUserResponse : BaseResponse
{
    public string Token { get; set; }
}

public class AuthorizeUserQueryValidator : AbstractValidator<AuthorizeUserQuery>
{
    public AuthorizeUserQueryValidator()
    {
        RuleFor(x => x.userId).GreaterThan(0);
        RuleFor(x => x.password).MinimumLength(6);
    }
}

public class AuthorizeUserQueryHandler : IRequestHandler<AuthorizeUserQuery, AuthorizeUserResponse>
{
    private readonly IAppDbContext _context;
    private readonly IJwtProvider _jwtProvider;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthorizeUserQueryHandler(
        IAppDbContext context, 
        IJwtProvider jwtProvider, 
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _jwtProvider = jwtProvider;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthorizeUserResponse> Handle(AuthorizeUserQuery request, CancellationToken cancellationToken)
    {
        User? user = _context.Users.WithRoleById(request.userId);
        if (user == null)
            throw new Common.Exceptions.ValidationException(
                nameof(request.userId),
                [new ErrorItem($"Unable to find user with id {request.userId}", ValidationErrorCode.EntityIdValidator)]);
        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.password) == PasswordVerificationResult.Failed)
            throw new Common.Exceptions.ValidationException(
                nameof(request.password), 
                [new ErrorItem($"Password is incorrect", ValidationErrorCode.PasswordIncorrectValidator)] );

        string jwt = _jwtProvider.GenerateToken(user);

        return new AuthorizeUserResponse { Token = jwt };
    }
}