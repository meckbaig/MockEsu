using MockEsu.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MockEsu.Domain.Entities;

public class Address : BaseEntity
{
    [Required]
    [ForeignKey(nameof(City))]
    public int CityId { get; set; }

    [Required]
    [ForeignKey(nameof(Street))]
    public int StreetId { get; set; }

    [Required]
    [ForeignKey(nameof(Region))]
    public int RegionId { get; set; }

    [Required]
    public string HouseName { get; set; }

    [Required]
    public int PorchNumber { get; set; }

    [Required]
    public string Apartment { get; set; }

    public City City { get; set; }

    public Street Street { get; set; }

    public Region Region { get; set; }
}
