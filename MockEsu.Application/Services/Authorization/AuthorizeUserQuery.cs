using FluentValidation;
using MediatR;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities;
using System.Security.Authentication;

namespace MockEsu.Application.Services.Authorization;

public record AuthorizeUserQuery : BaseRequest<AuthorizeUserResponse>
{
    public string login { get; set; }

    public int userId { get; set; }
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
        RuleFor(x => x.login).MinimumLength(5);
        RuleFor(x => x.userId).GreaterThan(0);
    }
}

public class AuthorizeUserQueryHandler : IRequestHandler<AuthorizeUserQuery, AuthorizeUserResponse>
{
    private readonly IAppDbContext _context;
    private readonly IJwtProvider _jwtProvider;

    public AuthorizeUserQueryHandler(IAppDbContext context, IJwtProvider jwtProvider)
    {
        _context = context;
        _jwtProvider = jwtProvider;
    }

    public async Task<AuthorizeUserResponse> Handle(AuthorizeUserQuery request, CancellationToken cancellationToken)
    {
        User? user = _context.Users.FirstOrDefault(k => k.Id == request.userId);
        if (user is null)
            throw new AuthenticationException($"Unable to find user with id {request.userId}");

        string jwt = _jwtProvider.GenerateToken(user);

        return new AuthorizeUserResponse { Token = jwt };
    }
}