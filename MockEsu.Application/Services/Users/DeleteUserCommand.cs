using AutoMapper;
using FluentValidation;
using MediatR;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities.Authentification;

namespace MockEsu.Application.Services.Users;

public record DeleteUserCommand : BaseRequest<DeleteUserResponse>
{
    public int id { get; set; }
}

public class DeleteUserResponse : BaseResponse
{

}

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, DeleteUserResponse>
{
    private readonly IAppDbContext _context;

    public DeleteUserCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<DeleteUserResponse> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        User? user = _context.Users.FirstOrDefault(u => u.Id == request.id);
        if (user == null)
            throw new KeyNotFoundException("Unable to find user");
        user.Deleted = true;
        _context.SaveChanges();
        return new DeleteUserResponse { };
    }
}
