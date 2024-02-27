using AutoMapper;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.Dtos;
using MockEsu.Domain.Entities;

namespace MockEsu.Application.DTOs.Kontragents;

public record KonragentPreviewDto : IBaseDto
{
    [Filterable(CompareMethod.Equals)]
    public int Id { get; set; }

    public string Name { get; set; }

    public string PhoneNumber { get; set; }

    public string DocumentNumber { get; set; }

    public string PersonalAccount { get; set; }

    [Filterable(CompareMethod.Equals)]
    public DateOnly? ContractDate { get; set; }

    [Filterable(CompareMethod.Equals)]
    public decimal Balance { get; set; }

    public string? AddressString { get; set; }

    public string RegionString { get; set; }

    public static Type GetOriginType()
    {
        return typeof(Kontragent);
    }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Kontragent, KonragentPreviewDto>()
                .ForMember(m => m.DocumentNumber, opt => opt.MapFrom(o => o.KontragentAgreement.DocumentNumber))
                .ForMember(m => m.PersonalAccount, opt => opt.MapFrom(o => o.KontragentAgreement.PersonalAccount))
                .ForMember(m => m.ContractDate, opt => opt.MapFrom(o => o.KontragentAgreement.ContractDate))
                .ForMember(m => m.Balance, opt => opt.MapFrom(o => o.KontragentAgreement.Balance))
                .ForMember(m => m.AddressString, opt => opt.MapFrom(o => AddressToString(o.Address)))
                .ForMember(m => m.RegionString, opt => opt.MapFrom(o => GetRegion(o.Address)))
                //.ReverseMap()
                ;
        }

        private static string GetRegion(Address? address)
        {
            if (address == null)
                return string.Empty;
            return address.Region.Name;
        }

        private static string AddressToString(Address? address)
        {
            if (address == null)
                return string.Empty;
            return
                $"г. {address.City.Name}, ул. {address.Street.Name}, д. {address.HouseName}, под. {address.PorchNumber}, кв. {address.Apartment}";
        }
    }
}