using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using MockEsu.Application.Common.BaseRequests.ListQuery;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.PaymentContracts;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Domain.Entities;

namespace MockEsu.Application.Services.PaymentContracts;

public record GetPaymentContractsQuery : BaseListQuery<GetPaymentContractsResponse>
{
    public override int skip { get; set; }
    public override int take { get; set; }
    public override string[]? filters { get; set; }
    public override string[]? orderBy { get; set; }
}

public class GetPaymentContractsResponse : BaseListQueryResponse<PaymentContractDto>
{
    public override IList<PaymentContractDto> Items { get; set; }
}

public class GetPaymentContractsQueryValidator : BaseListQueryValidator
    <GetPaymentContractsQuery, GetPaymentContractsResponse, PaymentContractDto, PaymentContract>
{
    public GetPaymentContractsQueryValidator(IMapper mapper) : base(mapper)
    {
    }
}

public class GetPaymentContractsQueryHandler : IRequestHandler<GetPaymentContractsQuery, GetPaymentContractsResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public GetPaymentContractsQueryHandler(IAppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetPaymentContractsResponse> Handle(GetPaymentContractsQuery request,
        CancellationToken cancellationToken)
    {
        var result = _context.PaymentContracts
            .AddFilters<PaymentContract, PaymentContractDto>(request.GetFilterExpressions())
            .AddOrderBy<PaymentContract, PaymentContractDto>(request.GetOrderExpressions())
            .Skip(request.skip).Take(request.take > 0 ? request.take : int.MaxValue)
            .ProjectTo<PaymentContractDto>(_mapper.ConfigurationProvider)
            .ToList();
        
        return new GetPaymentContractsResponse()
        {
            Items = result
        };
    }
}