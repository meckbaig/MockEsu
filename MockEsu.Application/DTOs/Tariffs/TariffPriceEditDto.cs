using MockEsu.Application.Common;
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

namespace MockEsu.Application.DTOs.Tariffs;

public record TariffPriceEditDto : BaseDto, IEntityWithId
{
    public int Id { get; init; }
    public string Name { get; set; }
    public int Price { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
             CreateMap<TariffPrice, TariffPriceEditDto>()
                .ForMember(m => m.Id, opt => opt.MapFrom(o => o.Id))
                .ReverseMap();
        }
    }
}
