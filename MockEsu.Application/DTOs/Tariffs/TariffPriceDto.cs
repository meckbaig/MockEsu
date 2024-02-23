using AutoMapper;
using FluentValidation;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.Dtos;
using MockEsu.Domain.Common;
using MockEsu.Domain.Entities.Traiffs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.DTOs.Tariffs;

public record TariffPriceDto : IBaseDto, IEntityWithId
{
    [Filterable(CompareMethod.Equals)]
    public int Id { get; init; }

    [Filterable(CompareMethod.Equals)]
    public string PriceName { get; set; }

    [Filterable(CompareMethod.Equals)]
    public int Price { get; set; }

    public static Type GetOriginType()
    {
        return typeof(TariffPrice);
    }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TariffPrice, TariffPriceDto>()
               .ForMember(m => m.Id, opt => opt.MapFrom(o => o.Id))
               .ForMember(m => m.PriceName, opt => opt.MapFrom(o => o.Name))
               .ReverseMap();
        }
    }
}
