﻿using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Domain.Entities;

namespace MockEsu.Application.DTOs.Users;

public record UserPreviewDto : BaseDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<User, UserPreviewDto>()
                .ForMember(m => m.Role, opt => opt.MapFrom(u => u.Role.Name));
        }
    }
}