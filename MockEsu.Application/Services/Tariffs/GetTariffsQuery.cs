using AutoMapper;
using FluentValidation;
using MediatR;
using MockEsu.Application.Common.BaseRequests.ListQuery;
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

    public GetTariffsQueryHandler(IAppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetTariffsResponse> Handle(GetTariffsQuery request, CancellationToken cancellationToken)
    {
        var list = _context.Tariffs.WithPrices()
            .AddFilters(request.GetFilterExpressions())
            .AddOrderBy(request.GetOrderExpressions())
            .Skip(request.skip).Take(request.take > 0 ? request.take : int.MaxValue)
            .Select(t => _mapper.Map<TariffDto>(t))
            .ToList();
        return new GetTariffsResponse { Items = list };
    }
}
