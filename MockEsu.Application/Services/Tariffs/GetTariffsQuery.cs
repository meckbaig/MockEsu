using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MockEsu.Application.Common.BaseRequests.ListQuery;
using MockEsu.Application.Common.Extensions.Caching;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Tariffs;
using MockEsu.Application.Extensions.DataBaseProvider;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Domain.Entities.Traiffs;

namespace MockEsu.Application.Services.Tariffs;

public record GetTariffsQuery : BaseListQuery<GetTariffsResponse>
{
    public override int skip { get; set; }
    public override int take { get; set; }
    public override string[]? filters { get; set; }
    public override string[]? orderBy { get; set; }
}

public class GetTariffsResponse : BaseListQueryResponse<TariffDto>
{
    public override IList<TariffDto> Items { get; set; }
}

public class GetTariffsQueryValidator : BaseListQueryValidator
    <GetTariffsQuery, GetTariffsResponse, TariffDto, Tariff>
{
    public GetTariffsQueryValidator(IMapper mapper) : base(mapper) { }
}

public class GetTariffsQueryHandler : IRequestHandler<GetTariffsQuery, GetTariffsResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    private readonly ICachedKeysProvider _cachedKeysProvider;

    public GetTariffsQueryHandler(IAppDbContext context, IMapper mapper, IDistributedCache cache, ICachedKeysProvider cachedKeysProvider)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
        _cachedKeysProvider = cachedKeysProvider;
    }

    public async Task<GetTariffsResponse> Handle(GetTariffsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tariffs.WithPrices()
            .AddFilters(request.GetFilterExpressions())
            .AddOrderBy(request.GetOrderExpressions())
            .Skip(request.skip).Take(request.take > 0 ? request.take : int.MaxValue);

        var projection = (List<Tariff> tariffs)
            => tariffs.Select(t => _mapper.Map<TariffDto>(t)).ToList();

        var list = await _cache.GetOrCreateAsync(
            _cachedKeysProvider,
            request.GetKey(),
            () => query.ToListAsync(),
            projection,
            cancellationToken);

        return new GetTariffsResponse { Items = list };
    }
}
