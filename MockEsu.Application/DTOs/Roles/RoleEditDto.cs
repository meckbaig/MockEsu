using AutoMapper;
using FluentValidation;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.DTOs.Tariffs;
using MockEsu.Application.Extensions.Validation;
using MockEsu.Domain.Entities.Authentification;

namespace MockEsu.Application.DTOs.Roles;

public record RoleEditDto : IBaseDto, IEditDto
{
    public string Name { get; set; }
    public HashSet<PermissionEditDto> Permissions { get; set; }


    public static Type GetOriginType()
    {
        return typeof(Role);
    }

    public static Type GetValidatorType()
    {
        return typeof(Validator);
    }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Role,  RoleEditDto>()
                .ForMember(r => r.Name, opt => opt.MapFrom(r => r.Name))
                .ForMember(r => r.Permissions, opt => opt.MapFrom(r => r.Permissions.Select(p => p.Id)));
            CreateMap<RoleEditDto, Role>()
                .ForMember(r => r.Name, opt => opt.MapFrom(r => r.Name))
                .ForMember(r => r.Permissions, opt => opt.MapFrom(r => r.Permissions));
        }
    }

    internal class Validator : AbstractValidator<RoleEditDto>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MinimumLength(2)
                .MaximumLength(100);
            RuleForEach(x => x.Permissions)
                .SetValidator(new PermissionEditDto.Validator());
        }
    }
}
