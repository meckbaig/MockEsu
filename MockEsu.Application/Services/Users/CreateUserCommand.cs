using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.Validation;
using MockEsu.Domain.Entities.Authentification;
using System.Data;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace MockEsu.Application.Services.Users;

public record CreateUserCommand : BaseRequest<CreateUserResponse>
{
    public string name { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    public string role { get; set; }
}

public class CreateUserResponse : BaseResponse
{
    public int Id { get; set; }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IAppDbContext context)
    {
        RuleFor(x => x.name).MinimumLength(4).MaximumLength(100);
        RuleFor(x => x.password).MinimumLength(6).MaximumLength(30);
        RuleFor(x => x.email).MaximumLength(320);
        RuleFor(x => x.email).EmailAddress();
        RuleFor(x => x.role).MustBeExistingRole(context);
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IAppDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public CreateUserCommandHandler(IAppDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        Role role = _context.Roles.FirstOrDefault(r => r.Name.ToLower() == request.role.ToLower())!;
        User user = new()
        {
            Name = request.name,
            Email = request.email,
            Role = role
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.password);
        _context.Users.Add(user);
        _context.SaveChanges();

        return new CreateUserResponse { Id = user.Id };
    }
}
