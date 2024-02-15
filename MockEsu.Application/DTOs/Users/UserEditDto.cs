using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using MockEsu.Application.Common.Dtos;
using MockEsu.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.DTOs.Users;

public record UserEditDto : BaseDto, IEditDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public int RoleId { get; set; }

    public static Type GetOriginType()
    {
        return typeof(User);
    }

    public static Type GetValidatorType()
    {
        throw new NotImplementedException();
    }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<User, UserEditDto>().ReverseMap();
            //CreateMap<UserEditDto, User>()
            //    .ForMember(dest => dest.RoleId, opt => opt.PreCondition(src => src.RoleId > 0))
            //    .ForAllMembers(opts =>
            //    {
            //        opts.Condition((src, dest, srcMember) 
            //            => srcMember != null);
            //    });
        }
    }
}
