using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.JsonPatch;
using MockEsu.Domain.Entities.Traiffs;

namespace MockEsu.Application.Services.Tariffs;

public record JsonPatchTariffsCommand : BaseRequest<JsonPatchTariffsResponse>
{
    public JsonPatchDocument<TariffDto> Patch { get; set; }

}

public class JsonPatchTariffsResponse : BaseResponse
{
    public List<TariffDto> Tariffs { get; set; }
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
        //var tariff = _context.Tariffs.Include(t => t.Prices).FirstOrDefault(t => t.Id == request.Id);
        //request.Patch.ApplyToSource(tariff, _mapper);
        //_context.SaveChanges();
        JsonPatchDocument<DbSet<Tariff>> jsonPatchDocument = request.Patch.ConvertToSourceDbSet<Tariff, TariffDto>(_mapper);
        jsonPatchDocument.ApplyTransactionToSource<DbSet<Tariff>, Tariff>(_context.Tariffs, _context);
        //_context.Tariffs
        //    .Include(t => t.Prices)
        //    .Where(t => t.Id == 1)
        //    .ExecuteUpdate(
        //        s => s.SetProperty(
        //            t => t.Prices.FirstOrDefault(p => p.Id == 1).Name,
        //            "new name"));

        var tariffs = _context.Tariffs
            .Include(t => t.Prices)
            .ProjectTo<TariffDto>(_mapper.ConfigurationProvider)
            .ToList();
        return new JsonPatchTariffsResponse { Tariffs = tariffs };
    }
}