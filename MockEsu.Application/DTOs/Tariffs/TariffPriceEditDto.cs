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
using FluentValidation;
using MockEsu.Application.Common.Attributes;

namespace MockEsu.Application.DTOs.Tariffs;

public record TariffPriceEditDto : IEditDto, IEntityWithId
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

    public static Type GetValidatorType()
    {
        return typeof(Validator);
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

    public class Validator : AbstractValidator<TariffPriceEditDto>
    {
        public Validator()
        {
            RuleFor(x => x.PriceName)
                .MinimumLength(3)
                .MaximumLength(100);
            RuleFor(x => x.Price)
                .GreaterThan(0);
        }
    }
}

