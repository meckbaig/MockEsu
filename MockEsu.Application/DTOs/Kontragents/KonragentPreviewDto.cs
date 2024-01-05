using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common;
using MockEsu.Application.Common.Attributes;
using MockEsu.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace MockEsu.Application.DTOs.Kontragents;

public record KonragentPreviewDto : BaseDto
{
    [Filterable(CompareMethod.Equals)]
    public int Id { get; set; }

    [Filterable(CompareMethod.CellContainsValue)]
    public string Name { get; set; }

    [Filterable(CompareMethod.CellContainsValue)]
    public string PhoneNumber { get; set; }

    [Filterable(CompareMethod.CellContainsValue)]
    public string DocumentNumber { get; set; }

    [Filterable(CompareMethod.CellContainsValue)]
    public string PersonalAccount { get; set; }

    [Filterable(CompareMethod.Date)]
    public DateOnly? ContractDate { get; set; }

    [Filterable(CompareMethod.Between)]
    public decimal Balance { get; set; }

    [Filterable(CompareMethod.CellContainsValue)]
    public string? AddressString { get; set; }

    [Filterable(CompareMethod.CellContainsValue)]
    public string RegionString { get; set; }
    public string CityName { get; set; }

    //private Dictionary<string, string> Properties { get; set; }

    //public IQueryable<TSource> AddFilters<TSource>(this IQueryable<TSource> source, IMapper mapper) where TSource : class
    //{
    //    var provider = mapper.ConfigurationProvider;
    //    Properties = new Dictionary<string, string>();
    //    foreach (var prop in this.GetType().GetProperties())
    //    {
    //        FilterableAttribute attribute = (FilterableAttribute)prop.GetCustomAttributes(true).FirstOrDefault(a => a.GetType() == typeof(FilterableAttribute));
    //        Properties.Add(prop.Name, GetSource<Kontragent, KonragentPreviewDto>(provider, prop.Name));
    //    }
    //    return source;
    //}

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
                .ForMember(m => m.CityName, opt => opt.MapFrom(o => o.Address.City.Name))
                .ReverseMap();
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
            return $"г. {address.City.Name}, ул. {address.Street.Name}, д. {address.HouseName}, под. {address.PorchNumber}, кв. {address.Apartment}";
        }

        //public class CustomResolver : IMemberValueResolver<Kontragent, KonragentPreviewDto, Address?, string?>
        //{
        //    public string Resolve(Kontragent source, KonragentPreviewDto destination, Address? sourceMember, string? destMember, ResolutionContext context)
        //    {
        //        return $"г. {sourceMember.City.Name}, ул. {sourceMember.Street.Name}, д. {sourceMember.HouseName}, под. {sourceMember.PorchNumber}, кв. {sourceMember.Apartment}";
        //    }
        //}
    }
}
