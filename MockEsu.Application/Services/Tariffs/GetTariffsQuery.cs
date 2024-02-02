using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.BaseRequests.JournalQuery;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
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

public class GetTariffsQueryValidator : BaseJournalQueryValidator
    <GetTariffsQuery, GetTariffsResponse, TariffDto, Tariff>
{
    public GetTariffsQueryValidator(IMapper mapper) : base(mapper)
    {

    }
}

public class GetTariffsQueryHandler : IRequestHandler<GetTariffsQuery, GetTariffsResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public GetTariffsQueryHandler(IAppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetTariffsResponse> Handle(GetTariffsQuery request, CancellationToken cancellationToken)
    {
        var list = _context.Tariffs
            .Include(t => t.Prices)
            .AddFilters<Tariff, TariffDto>(request.GetFilterExpressions())
            .AddOrderBy<Tariff, TariffDto>(request.GetOrderExpressions())
            .Skip(request.skip).Take(request.take > 0 ? request.take : int.MaxValue)
            .ProjectTo<TariffDto>(_mapper.ConfigurationProvider)
            .ToList();
        return new GetTariffsResponse { Items = list };
    }
}
