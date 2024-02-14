using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.DataBaseProvider;
using MockEsu.Application.Extensions.JsonPatch;
using MockEsu.Domain.Entities;
using MockEsu.Domain.Entities.Traiffs;
using StackExchange.Redis;

namespace MockEsu.Application.Services.Tariffs;

public record JsonPatchTariffsCommand : BaseRequest<JsonPatchTariffsResponse>
{
    public JsonPatchDocument<TariffDto> Patch { get; set; }

}

public class JsonPatchTariffsResponse : BaseResponse
{
    public List<TariffEditDto> Tariffs { get; set; }
}

public class JsonPatchTariffsCommandValidator : AbstractValidator<JsonPatchTariffsCommand>
{
    public JsonPatchTariffsCommandValidator()
    {

    }
}

public class JsonPatchTariffsCommandHandler : IRequestHandler<JsonPatchTariffsCommand, JsonPatchTariffsResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public JsonPatchTariffsCommandHandler(IAppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<JsonPatchTariffsResponse> Handle(JsonPatchTariffsCommand request, CancellationToken cancellationToken)
    {
        request.Patch.ApplyDtoTransactionToSource(_context.Tariffs, _mapper.ConfigurationProvider);

        var tariffs = _context.Tariffs.WithPrices().AsNoTracking()
            .Select(t => _mapper.Map<TariffEditDto>(t))
            //.ProjectTo<TariffDto>(_mapper.ConfigurationProvider)
            .ToList();
        tariffs.ForEach(t => t.PricePoints = t.PricePoints.OrderBy(p => p.Id).ToList());
        return new JsonPatchTariffsResponse { Tariffs = tariffs };
    }
}