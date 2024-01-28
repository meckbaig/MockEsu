using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities;
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
        RuleFor(x => x.name).MinimumLength(4);
        RuleFor(x => x.password).MinimumLength(6);
        RuleFor(x => x.email).Must(BeValidEmail)
            .WithMessage((q, p) => $"'{p}' is not valid email")
            .WithErrorCode("NotValidEmailValidator");
        RuleFor(x => x.role).Must((q, p) => BeExistingRole(p, context))
            .WithMessage((q, p) => $"'{p} is not existing role'")
            .WithErrorCode("NotExistingRoleValidator");
    }

    private bool BeExistingRole(string role, IAppDbContext context)
    {
        return context.Roles.FirstOrDefault(r => r.Name.Equals(role, StringComparison.CurrentCultureIgnoreCase)) != null;
    }

    private bool BeValidEmail(string email)
    {
        Regex regex = new Regex(@"^[\w-]+(\.[\w-]+)*@([\w-]+\.)+[a-zA-Z]{2,7}$");
        return regex.IsMatch(email);
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
