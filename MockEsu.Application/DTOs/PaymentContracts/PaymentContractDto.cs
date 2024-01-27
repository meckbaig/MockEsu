﻿using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Domain.Entities;

namespace MockEsu.Application.DTOs.PaymentContracts;

public record PaymentContractDto : BaseDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Day { get; set; }
    public int Rent { get; set; }
    public int Frequency { get; set; }
    public int Print { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<PaymentContract, PaymentContractDto>();
        }
    }
    
}