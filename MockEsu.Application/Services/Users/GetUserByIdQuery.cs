using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Users;
using MockEsu.Domain.Entities;

namespace MockEsu.Application.Services.Users;

public record GetUserByIdQuery : BaseRequest<GetUserByIdResponse>
{
    public int id { get; set; }
}

public class GetUserByIdResponse : BaseResponse
{
    public UserPreviewDto User { get; set; }
}

public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(x => x.id).GreaterThan(0);
    }
}

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, GetUserByIdResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public GetUserByIdQueryHandler(IAppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetUserByIdResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        UserPreviewDto user = _mapper.Map<UserPreviewDto>(
            _context.Users.Include(u => u.Role)
                .FirstOrDefault(u => u.Id == request.id));
        return new GetUserByIdResponse { User = user };
    }
}
