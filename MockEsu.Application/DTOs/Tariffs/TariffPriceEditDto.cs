using MockEsu.Domain.Entities.Traiffs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MockEsu.Domain.Common;
using MockEsu.Domain.Entities;
using MockEsu.Application.Common.Dtos;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.JsonPatch;

namespace MockEsu.Application.DTOs.Tariffs;

public record TariffPriceEditDto : BaseDto, IEntityWithId, IEditDto
{
    public int Id { get; init; }
    public string PriceName { get; set; }
    public int Price { get; set; }

    public static Type GetOriginType()
    {
        return typeof(TariffPrice);
    }

    private class Mapping : Profile
    {
        public Mapping()
        {
             CreateMap<TariffPrice, TariffPriceEditDto>()
                .ForMember(m => m.Id, opt => opt.MapFrom(o => o.Id))
                .ForMember(m => m.PriceName, opt => opt.MapFrom(o => o.Name))
                .ReverseMap();
        }
    }
}
