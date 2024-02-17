using AutoMapper;
using FluentValidation;
using MediatR;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities.Authentification;

namespace MockEsu.Application.Services.Roles;

public record CreateRoleCommand : BaseRequest<CreateRoleResponse>
{
    public string name { get; set; }
    public string[]? permissions { get; set; }
}

public class CreateRoleResponse : BaseResponse
{
    public string Role { get; set; }
}

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.name).MinimumLength(4);
    }
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, CreateRoleResponse>
{
    private readonly IAppDbContext _context;

    public CreateRoleCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<CreateRoleResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (_context.Roles.FirstOrDefault(r => r.Name.ToLower() == request.name.ToLower()) != null)
            throw new ArgumentException($"'{request.name}' - role already exists");
        Role role = new() 
        {
            Name = request.name,
            Permissions = request.permissions ?? Enumerable.Empty<string>().ToArray()
        };
        _context.Roles.Add(role);
        _context.SaveChanges();
        return new CreateRoleResponse { Role = role.Name };
    }
}
