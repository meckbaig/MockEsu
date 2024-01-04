using MockEsu.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MockEsu.Domain.Entities;

public class KontragentAgreement : BaseEntity
{
    [Required]
    public string DocumentNumber { get; set; }

    [Required]
    public string PersonalAccount { get; set; }

    public DateOnly? ContractDate { get; set; }

    [Required]
    public decimal Balance { get; set; }

    [Required]
    [ForeignKey(nameof(Organization))]
    public int OrganizationId { get; set; }

    [Required]
    [ForeignKey(nameof(Kontragent))]
    public int KontragentId { get; set; }

    [Required]
    [ForeignKey(nameof(PaymentContract))]
    public int PaymentContractId { get; set; }

    public Organization Organization { get; set; }

    public Kontragent Kontragent { get; set; }

    public PaymentContract PaymentContract { get; set; }
}
