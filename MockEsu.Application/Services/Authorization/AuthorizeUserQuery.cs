using FluentValidation;
using MediatR;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;

namespace MockEsu.Application.Services.Authorization;

public record AuthorizeUserQuery : BaseRequest<AuthorizeUserResponse>
{
    public string login { get; set; }

    public int clientId { get; set; }
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
        RuleFor(x => x.clientId).GreaterThan(0);
    }
}

public class AuthorizeUserQueryHandler : IRequestHandler<AuthorizeUserQuery, AuthorizeUserResponse>
{
    ///TODO: store securily
    private const string TokenSecret = "MockEsuBackend123456565644665456";

    private static readonly TimeSpan TokenLifeTime = TimeSpan.FromMinutes(1);
    private readonly IAppDbContext _context;

    public AuthorizeUserQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<AuthorizeUserResponse> Handle(AuthorizeUserQuery request, CancellationToken cancellationToken)
    {
        Kontragent kontragent = _context.Kontragents.FirstOrDefault(k => k.Id == request.clientId);
        if (kontragent is null)
            throw new AuthenticationException($"Unable to find user with id {request.clientId}");

        var tokenHandler = new JsonWebTokenHandler();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, request.login),
            new(JwtRegisteredClaimNames.PhoneNumber, kontragent.PhoneNumber),
            new("userId", kontragent.Id.ToString()),
            new(ClaimTypes.Role, request.login == "meckbaig" ? "admin" : "user")
        };

        var tokenDesctiptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TokenLifeTime),
            Issuer = "meckbaig",
            Audience = "users",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TokenSecret)),
                SecurityAlgorithms.HmacSha256)
        };
        var token = tokenHandler.CreateToken(tokenDesctiptor);
        var jwt = tokenHandler.ReadJsonWebToken(token);
        return new AuthorizeUserResponse { Token = jwt.UnsafeToString() };
    }
}