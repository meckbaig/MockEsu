using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using MediatR;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Application.Extensions.DataBaseProvider;
using MockEsu.Domain.Entities;

namespace MockEsu.Application.Services.Kontragents;

public record GetFromElasticQuery : BaseRequest<GetFromElasticResponse>
{
    public string searchString { get; set; }
}

public class GetFromElasticResponse : BaseResponse
{
	public List<KonragentPreviewDto> Items { get; set; }
}

public class GetFromElasticQueryValidator : AbstractValidator<GetFromElasticQuery>
{
    public GetFromElasticQueryValidator()
    {
        
    }
}

public class GetFromElasticQueryHandler : IRequestHandler<GetFromElasticQuery, GetFromElasticResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IElasticSearchClient _searchClient;

    public GetFromElasticQueryHandler(IAppDbContext context, IElasticSearchClient searchClient, IMapper mapper)
    {
        _context = context;
        _searchClient = searchClient;
        _mapper = mapper;
    }

    public async Task<GetFromElasticResponse> Handle(GetFromElasticQuery request, CancellationToken cancellationToken)
    {
        //using HttpClient httpClient = new HttpClient();
        //httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "ZWxhc3RpYzpTTGRDKkJQZllzX203aD1sZ0Q4Qg==");
        //var res = await httpClient.GetAsync("https://host.docker.internal:9202/");
        //string answer = await res.Content.ReadAsStringAsync();

        //var kontragents = _context.Kontragents.FullData().ProjectTo<KonragentPreviewDto>(_mapper.ConfigurationProvider).ToList();
        //await _searchClient.AddAsync(kontragents);

        var result = await _searchClient.SearchAsync<KonragentPreviewDto>(request.searchString);

        return new GetFromElasticResponse { Items = result };
    }
}
