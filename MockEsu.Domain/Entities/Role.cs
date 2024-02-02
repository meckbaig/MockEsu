using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Domain.Entities;

public class Role : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    public string[] Permissions { get; set; }

    public List<User> Users { get; set; }
}
