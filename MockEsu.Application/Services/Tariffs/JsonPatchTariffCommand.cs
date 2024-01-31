using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using MockEsu.Application.Common;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Tariffs;
using MockEsu.Application.DTOs.Users;
using MockEsu.Domain.Entities.Traiffs;
using MockEsu.Application.Extensions.JsonPatch;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using AutoMapper.QueryableExtensions;

namespace MockEsu.Application.Services.Tariffs;

public record JsonPatchTariffCommand : BaseRequest<JsonPatchTariffResponse>
{
    public int Id { get; set; }
    public JsonPatchDocument<TariffDto> Patch { get; set; }

}

public class JsonPatchTariffResponse : BaseResponse
{
    public List<TariffDto> Tariffs { get; set; }
}

public class JsonPatchTariffCommandValidator : AbstractValidator<JsonPatchTariffCommand>
{
    public JsonPatchTariffCommandValidator()
    {
        
    }
}

public class JsonPatchTariffCommandHandler : IRequestHandler<JsonPatchTariffCommand, JsonPatchTariffResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public JsonPatchTariffCommandHandler(IAppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<JsonPatchTariffResponse> Handle(JsonPatchTariffCommand request, CancellationToken cancellationToken)
    {
        var tariff = _context.Tariffs.Include(t => t.Prices).FirstOrDefault(t => t.Id == request.Id);
        request.Patch.ApplyToSource(tariff, _mapper);
        _context.SaveChanges();

        var tariffs = _context.Tariffs
            .Include(t => t.Prices)
            .ProjectTo<TariffDto>(_mapper.ConfigurationProvider)
            .ToList();
        return new JsonPatchTariffResponse { Tariffs = tariffs };
    }
}

public record TariffDto : BaseDto
{
    public string Name { get; set; }

    public List<TariffPriceEditDto> Prices { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Tariff, TariffDto>()
                .ForMember(m => m.Prices, opt => opt.MapFrom(o => o.Prices))
                .ReverseMap();
        }
    }
}