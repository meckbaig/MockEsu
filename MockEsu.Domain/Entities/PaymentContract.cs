using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Domain.Entities;

public class PaymentContract : BaseEntity
{
    [Required]
    public string Name { get; set; }
    public int Day { get; set; }
    public int Rent { get; set; }
    public int Frequency { get; set; }
    public int Print { get; set; }
}
