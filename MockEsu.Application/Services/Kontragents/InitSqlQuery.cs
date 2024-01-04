using AutoMapper;
using FluentValidation;
using MediatR;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;

namespace MockEsu.Application.Services.Kontragents;

public record InitSqlQuery : BaseRequest<InitSqlResponse>
{

}

public class InitSqlResponse : BaseResponse
{

}

public class InitSqlQueryHandler : IRequestHandler<InitSqlQuery, InitSqlResponse>
{
    private readonly IAppDbContext _context;
    //private readonly IMapper _mapper;

    public InitSqlQueryHandler(IAppDbContext context)
    {
        _context = context;
        //_mapper = mapper;
    }

    public async Task<InitSqlResponse> Handle(InitSqlQuery request, CancellationToken cancellationToken)
    {
        _context.Kontragents.FirstOrDefault();
        return new InitSqlResponse();
    }
}
