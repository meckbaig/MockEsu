using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Domain.Entities;
using MockEsu.Infrastructure.Extensions;

namespace MockEsu.Application.Services.Kontragents;

public record GetKontragentsQuery : BaseRequest<GetKontragentsResponse>//, IJournalQuery<KonragentPreviewDto>
{
    public int skip { get; set; }
    public int take { get; set; }
    
    public string[]? filters { get; set; }
}

public class GetKontragentsResponse : BaseResponse
{
    public IList<KonragentPreviewDto> Konragents { get; set; }
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
                //.Where(k => k.Id.Equals(30))
                .AddFilters<Kontragent, KonragentPreviewDto>(_mapper, request.filters)
                .Skip(request.skip).Take(request.take).OrderBy(k => k.Id)
                .ProjectTo<KonragentPreviewDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        return new GetKontragentsResponse()
        {
            Konragents = qwerty
        };
    }
}
