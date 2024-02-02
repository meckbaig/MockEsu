using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Users;
using MockEsu.Application.Extensions.JsonPatch;
using MockEsu.Domain.Entities;

namespace MockEsu.Application.Services.Users;

public record UpdateUserCommand : BaseRequest<UpdateUserResponse>
{
    //public UserEditDto Item { get; set; }
    public int Id { get; set; }
    public JsonPatchDocument<UserEditDto> Patch { get; set; }
}

public class UpdateUserResponse : BaseResponse
{
    public UserPreviewDto Item { get; set; }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator(IAppDbContext context)
    {
        //RuleFor(x => x.Item.Id).GreaterThan(0);
        //RuleFor(x => x.Item.Name).MinimumLength(4).MaximumLength(100);
        //RuleFor(x => x.Item.Password).MinimumLength(6).MaximumLength(30);
        //RuleFor(x => x.Item.Email).MaximumLength(320);
        //RuleFor(x => x.Item.Email).EmailAddress();
        //RuleFor(x => x.Item.RoleId).MustBeExistingRoleOrNull(context);
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
{
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public UpdateUserCommandHandler(IAppDbContext context, IMapper mapper, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
    }

    public async Task<UpdateUserResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        User? user = _context.Users.Include(u => u.Role)
            .FirstOrDefault(u => u.Id == request.Id);
        if (user == null)
            throw new KeyNotFoundException("Unable to find user");

        request.Patch.ApplyToSource(user, _mapper);
        _context.SaveChanges();

        return new UpdateUserResponse
        {
            Item = _mapper.Map<UserPreviewDto>(user)
        };
        ////TODO: move to validator
        //User? user = _context.Users.Include(u => u.Role)
        //    .FirstOrDefault(u => u.Id == request.Item.Id);
        //if (user == null)
        //    throw new KeyNotFoundException("Unable to find user");

        //user = MapChanges(request, user);
        //_context.SaveChanges();

        //return new UpdateUserResponse { Item = _mapper.Map<UserPreviewDto>(user) };
    }


    private User MapChanges(UserEditDto item, User user)
    {
        user = _mapper.Map(item, user);
        if (item.Password != null)
            user.PasswordHash = _passwordHasher.HashPassword(user, item.Password);
        return user;
    }
}
