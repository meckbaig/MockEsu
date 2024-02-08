using MockEsu.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MockEsu.Domain.Entities;

public class Organization : BaseEntity
{
    [Required]
    public string Name { get; set; }

    [Required]
    [ForeignKey(nameof(Address))]
    public int AddressId { get; set; }

    public Address Address { get; set; } 

    //public List<OrganizationInRegion> OrganizationInRegions { get; set; } = [];
    public List<Region> Regions { get; set; }
}
