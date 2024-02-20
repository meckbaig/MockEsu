using AutoMapper;
using MockEsu.Application.Common.Dtos;
using MockEsu.Domain.Entities.Authentification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.DTOs.Roles;

public record RolePreviewDto : BaseDto
{
    public string Name { get; set; }
    public string[] PermissionsNames { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Role, RolePreviewDto>()
                .ForMember(r => r.Name, opt => opt.MapFrom(r => r.Name))
                .ForMember(r => r.PermissionsNames, opt => opt.MapFrom(r => r.Permissions.Select(p => p.Name)));
        }
    }
}
