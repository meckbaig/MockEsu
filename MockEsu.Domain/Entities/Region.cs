using MockEsu.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace MockEsu.Domain.Entities;

public class Region : BaseEntity
{
    [Required]
    public string Name { get; set; }

    //public List<OrganizationInRegion> OrganizationsInRegion { get; set; } = [];
    public List<Organization> Organizations { get; set;}
}
