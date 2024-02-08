using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Domain.Entities;

public class OrganizationInRegion
{
    [Required]
    [ForeignKey(nameof(Organization))]
    public int OrganizationId { get; set; }

    public Organization Organization { get; set; }

    [Required]
    [ForeignKey(nameof(Region))]
    public int RegionId { get; set; }

    public Region Region { get; set; }
}
