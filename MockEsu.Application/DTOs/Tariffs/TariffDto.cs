using AutoMapper;
using FluentValidation;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.Dtos;
using MockEsu.Domain.Entities.Traiffs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.DTOs.Tariffs;

public record TariffDto : IBaseDto
{
    public string Name { get; set; }

    [Filterable(CompareMethod.Nested)]
    public List<TariffPriceDto> PricePoints { get; set; }

    public static Type GetOriginType() => typeof(Tariff);

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Tariff, TariffDto>()
                .ForMember(m => m.PricePoints, opt => opt.MapFrom(o => o.Prices));
            CreateMap<TariffDto, Tariff>()
                .ForMember(m => m.Prices, opt => opt.MapFrom(o => o.PricePoints))
                .ForMember(m => m.Created, opt => opt.Ignore())
                .ForMember(m => m.LastModified, opt => opt.Ignore());
        }
    }
}
