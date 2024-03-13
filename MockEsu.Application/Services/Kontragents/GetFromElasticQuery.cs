using AutoMapper;
using FluentValidation;
using MediatR;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities;

namespace MockEsu.Application.Services.Kontragents;

public record GetFromElasticQuery : BaseRequest<GetFromElasticResponse>
{
    public string searchString { get; set; }
}

public class GetFromElasticResponse : BaseResponse
{
	
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
    private readonly IElasticSearchClient _searchClient;

    public GetFromElasticQueryHandler(IAppDbContext context, IElasticSearchClient searchClient)
    {
        _context = context;
        _searchClient = searchClient;
    }

    public async Task<GetFromElasticResponse> Handle(GetFromElasticQuery request, CancellationToken cancellationToken)
    {
        await _searchClient.SearchAsync<Kontragent>(request.searchString);

        return new GetFromElasticResponse { };
    }
}
