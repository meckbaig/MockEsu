using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MockEsu.Application.Common.BaseRequests.ListQuery;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Application.Extensions.DataBaseProvider;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Domain.Entities;
using Newtonsoft.Json;

namespace MockEsu.Application.Services.Kontragents;

public record GetKontragentsQuery : BaseListQuery<GetKontragentsResponse>
{
    //public string qwerty { get; set; }
    public override int skip { get; set; }
    public override int take { get; set; }
    public override string[]? filters { get; set; }
    public override string[]? orderBy { get; set; }
}

public class GetKontragentsResponse : BaseListQueryResponse<KonragentPreviewDto>
{
    public override IList<KonragentPreviewDto> Items { get; set; }
}

public class GetKontragentsQueryValidator : BaseListQueryValidator
    <GetKontragentsQuery, GetKontragentsResponse, KonragentPreviewDto, Kontragent>
{
    public GetKontragentsQueryValidator(IMapper mapper) : base(mapper)
    {

    }
}

internal class GetKontragentsQueryHandler : IRequestHandler<GetKontragentsQuery, GetKontragentsResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    
    public GetKontragentsQueryHandler(IAppDbContext context, IMapper mapper, IDistributedCache cache)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<GetKontragentsResponse> Handle(GetKontragentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Kontragents.FullData()
            .AddFilters(request.GetFilterExpressions())
            .AddOrderBy(request.GetOrderExpressions())
            .Skip(request.skip).Take(request.take > 0 ? request.take : int.MaxValue)
            .ProjectTo<KonragentPreviewDto>(_mapper.ConfigurationProvider);

        var result = await _cache.GetOrCreate(
            request.GetKey(), 
            () => query.ToList(), 
            cancellationToken);
        
        return new GetKontragentsResponse
        {
            Items = result
        };
    }
}