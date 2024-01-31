using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Domain.Entities.Traiffs;

public class Tariff : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    public List<TariffPrice> Prices { get; set; } = [];
}
