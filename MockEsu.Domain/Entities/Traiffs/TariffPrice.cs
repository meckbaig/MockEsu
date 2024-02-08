using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Domain.Entities.Traiffs;

public class TariffPrice : BaseEntity, INonDelitableEntity
{
    [Required]
    public string Name { get; set; }

    [Required] 
    public int Price { get; set; }

    [Required]
    [ForeignKey(nameof(Tariff))]
    public int TariffId { get; set; }

    public Tariff Tariff { get; set; }

    public bool Deleted { get; set; }

    public TariffPrice() { }
    public TariffPrice(int tariffId)
    {
        TariffId = tariffId;
    }
}
