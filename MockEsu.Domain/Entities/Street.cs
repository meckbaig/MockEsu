using MockEsu.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MockEsu.Domain.Entities;

public class Street : BaseEntity
{
    [Required]
    public string Name { get; set; }

    [Required]
    [ForeignKey(nameof(City))]
    public int CityId { get; set; }

    public City City { get; set; }
}
