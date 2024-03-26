using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.BaseRequests.ListQuery;
using MockEsu.Application.Common.Extensions.Caching;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Users;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Domain.Entities.Authentification;

namespace MockEsu.Application.Services.Users;

public record GetUsersQuery : BaseListQuery<GetUsersResponse>
{
    public override int skip { get; set; }
    public override int take { get; set; }
    public override string[]? filters { get; set; }
    public override string[]? orderBy { get; set; }
}

public class GetUsersResponse : BaseListQueryResponse<UserPreviewDto>
{
    public override IList<UserPreviewDto> Items { get; set; }
}

public class GetUsersQueryValidator : BaseListQueryValidator
    <GetUsersQuery, GetUsersResponse, UserPreviewDto, User>
{
    public GetUsersQueryValidator(IMapper mapper) : base(mapper) { }
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, GetUsersResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;

    public GetUsersQueryHandler(IAppDbContext context, IMapper mapper, IDistributedCache cache)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<GetUsersResponse> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users
            .Include(u => u.Role)
            .AddFilters(request.GetFilterExpressions())
            .AddOrderBy(request.GetOrderExpressions())
            .Skip(request.skip).Take(request.take > 0 ? request.take : int.MaxValue)
            .ProjectTo<UserPreviewDto>(_mapper.ConfigurationProvider);

        //var result = await _cache.GetOrCreate(
        //    request.GetKey(),
        //    () => query.ToList(),
        //    cancellationToken);

        var result = query.ToList();

        return new GetUsersResponse { Items = result };
    }
}
