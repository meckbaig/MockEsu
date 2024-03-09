using AutoMapper;
using FluentValidation;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.UnitTests.Common.Entities;

namespace MockEsu.Application.UnitTests.Common.DTOs;

public record TestNestedEntityEditDto : IEditDto
{
    public int Id { get; set; }

    public string NestedName { get; set; }

    public int Number { get; set; }

    public static Type GetOriginType()
    {
        return typeof(TestNestedEntity);
    }

    public static Type GetValidatorType()
    {
        return typeof(Validator);
    }

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TestNestedEntityEditDto, TestNestedEntity>()
                .ForMember(m => m.Id, opt => opt.MapFrom(o => o.Id))
                .ForMember(m => m.Name, opt => opt.MapFrom(o => o.NestedName))
                .ForMember(m => m.Number, opt => opt.MapFrom(o => o.Number));
        }
    }

    internal class Validator : AbstractValidator<TestNestedEntityEditDto>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0);
            RuleFor(x => x.NestedName)
                .NotEmpty()
                .MinimumLength(2)
                .MaximumLength(100);
            RuleFor(x => x.Number)
                .NotNull();
        }
    }
}
