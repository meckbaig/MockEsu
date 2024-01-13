using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.BaseRequests.JournalQuery;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Application.Extensions.JournalFilters;
using MockEsu.Domain.Entities;

namespace MockEsu.Application.Services.Kontragents;

public record GetKontragentsQuery : BaseJournalQuery<GetKontragentsResponse>
{
    public string qwerty { get; set; }
}

public class GetKontragentsResponse : BaseJournalQueryResponse<KonragentPreviewDto>
{
    public override IList<KonragentPreviewDto> Journal { get; set; }
}

public class GetKontragentsQueryValidator : BaseJournalQueryValidator
    <GetKontragentsQuery, GetKontragentsResponse, KonragentPreviewDto, Kontragent>
{
    public GetKontragentsQueryValidator(IMapper mapper) : base(mapper)
    {
        RuleFor(x => x.qwerty).MinimumLength(10).WithMessage("qwerty must be longer than 9 symbols");
    }
}

public class GetKontragentsQueryHandler : IRequestHandler<GetKontragentsQuery, GetKontragentsResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public GetKontragentsQueryHandler(IAppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetKontragentsResponse> Handle(GetKontragentsQuery request, CancellationToken cancellationToken)
    {
        var qwerty = await _context.Kontragents
                .Include(k => k.KontragentAgreement)
                .Include(k => k.Address).ThenInclude(a => a.City)
                .Include(k => k.Address).ThenInclude(a => a.Street)
                .Include(k => k.Address).ThenInclude(a => a.Region)
                .AddFilters<Kontragent, KonragentPreviewDto>(_mapper.ConfigurationProvider, request.filters)
                .Skip(request.skip).Take(request.take).OrderBy(k => k.Id)
                .ProjectTo<KonragentPreviewDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        return new GetKontragentsResponse()
        {
            Journal = qwerty
        };
    }
}
