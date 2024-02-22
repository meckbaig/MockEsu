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

public record TariffEditDto : IBaseDto, IEditDto
{
    public string Name { get; set; }

    [Filterable(CompareMethod.ById)]
    public List<TariffPriceEditDto> PricePoints { get; set; }

    public static Type GetOriginType() => typeof(Tariff);

    public static Type GetValidatorType() => typeof(Validator);

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Tariff, TariffEditDto>()
                .ForMember(m => m.PricePoints, opt => opt.MapFrom(o => o.Prices));
            CreateMap<TariffEditDto, Tariff>()
                .ForMember(m => m.Prices, opt => opt.MapFrom(o => o.PricePoints))
                .ForMember(m => m.Created, opt => opt.Ignore())
                .ForMember(m => m.LastModified, opt => opt.Ignore());
        }
    }

    internal class Validator : AbstractValidator<TariffEditDto>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MinimumLength(2)
                .MaximumLength(100);
            RuleForEach(x => x.PricePoints)
                .SetValidator(new TariffPriceEditDto.Validator());
        }
    }
}
