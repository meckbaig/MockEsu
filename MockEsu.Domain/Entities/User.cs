using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Domain.Entities;

public class User : BaseEntity, INonDelitableEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    [StringLength(256)]
    public string PasswordHash { get; set; }

    [Required]
    [StringLength(320)]
    public string Email { get; set; }

    [Required]
    [ForeignKey(nameof(Role))]
    public int RoleId { get; set; }
    [Required]
    public bool Deleted { get; set; }

    public Role Role { get; set; }
}
