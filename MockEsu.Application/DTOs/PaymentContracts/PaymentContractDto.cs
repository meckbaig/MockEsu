using AutoMapper;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Domain.Entities;

namespace MockEsu.Application.DTOs.PaymentContracts;

public record PaymentContractDto : IBaseDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Day { get; set; }
    public int Rent { get; set; }
    public int Frequency { get; set; }
    public int Print { get; set; }

    public static Type GetOriginType()
        => typeof(PaymentContract);

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<PaymentContract, PaymentContractDto>();
        }
    }
    
}