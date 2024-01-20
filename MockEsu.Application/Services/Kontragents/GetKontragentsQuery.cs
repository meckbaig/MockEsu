using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MockEsu.Application.Common.BaseRequests.JournalQuery;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Domain.Entities;
using Newtonsoft.Json;

namespace MockEsu.Application.Services.Kontragents;

public record GetKontragentsQuery : BaseListQuery<GetKontragentsResponse>
{
    //public string qwerty { get; set; }
}

public class GetKontragentsResponse : BaseListQueryResponse<KonragentPreviewDto>
{
    public override IList<KonragentPreviewDto> Items { get; set; }
}

public class GetKontragentsQueryValidator : BaseJournalQueryValidator
    <GetKontragentsQuery, GetKontragentsResponse, KonragentPreviewDto, Kontragent>
{
    public GetKontragentsQueryValidator(IMapper mapper) : base(mapper)
    {
        //RuleFor(x => x.qwerty).MinimumLength(10);
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
        var query = _context.Kontragents
            .Include(k => k.KontragentAgreement)
            .Include(k => k.Address).ThenInclude(a => a.City)
            .Include(k => k.Address).ThenInclude(a => a.Street)
            .Include(k => k.Address).ThenInclude(a => a.Region)
            .AddFilters<Kontragent, KonragentPreviewDto>(request.GetFilterExpressions())
            .Skip(request.skip).Take(request.take)
            .AddOrderBy<Kontragent, KonragentPreviewDto>(request.GetOrderExpressions())
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