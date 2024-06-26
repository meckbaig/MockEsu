﻿using MockEsu.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MockEsu.Domain.Entities;

public class Kontragent : BaseEntity
{
    [Required]
    public string Name { get; set; }

    [Required]
    public string PhoneNumber { get; set; }

    [ForeignKey(nameof(Address))]
    public int? AddressId { get; set; } 

    public Address? Address { get; set; }

    public KontragentAgreement KontragentAgreement { get; set; }
}

