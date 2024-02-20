using AutoMapper;
using MockEsu.Application.Common.Dtos;
using MockEsu.Domain.Entities.Authentification;

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