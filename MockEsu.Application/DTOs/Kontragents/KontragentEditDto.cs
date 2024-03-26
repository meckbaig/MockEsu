using AutoMapper;
using FluentValidation;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.DTOs.Tariffs;
using MockEsu.Domain.Entities;
using MockEsu.Domain.Entities.Traiffs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.DTOs.Kontragents;

public record KontragentEditDto : IEditDto
{
    public string LivingApartment { get; set; }
    public string StreetName { get; set; }

    public static Type GetOriginType() => typeof(Kontragent);

    public static Type GetValidatorType() => typeof(Validator);
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<KontragentEditDto, Kontragent>()
                .ForPath(m => m.Address.Apartment, opt => opt.MapFrom(o => o.LivingApartment))
                .ForPath(m => m.Address.Street.Name, opt => opt.MapFrom(o => o.StreetName));
        }
    }

    internal class Validator : AbstractValidator<KontragentEditDto>
    {
        public Validator()
        {
            RuleFor(x => x.LivingApartment)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(10);
            RuleFor(x => x.StreetName)
                .NotEmpty()
                .MinimumLength(2)
                .MaximumLength(100);
        }
    }
}
