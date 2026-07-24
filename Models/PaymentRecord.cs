using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("payment_records")]
public class PaymentRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("appointment_id")]
    public int? AppointmentId { get; set; }

    [Column("client_package_id")]
    public int? ClientPackageId { get; set; }

    [Column("discount_id")]
    public int? DiscountId { get; set; }

    [Column("original_amount", TypeName = "decimal(10,2)")]
    public decimal OriginalAmount { get; set; }

    [Column("final_amount", TypeName = "decimal(10,2)")]
    public decimal FinalAmount { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("payment_method")]
    public string PaymentMethod { get; set; } = PaymentMethods.Cash;

    [Required]
    [MaxLength(30)]
    [Column("payment_type")]
    public string PaymentType { get; set; } = PaymentTypes.SingleSession;

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = PaymentStatus.Pending;

    [MaxLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    [Column("recorded_by")]
    public int RecordedBy { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("ClientProfileId")]
    public ClientProfile ClientProfile { get; set; } = null!;

    [ForeignKey("AppointmentId")]
    public Appointment? Appointment { get; set; }

    [ForeignKey("DiscountId")]
    public Discount? Discount { get; set; }
}

public static class PaymentMethods
{
    public const string Cash = "cash";
    public const string Transfer = "transfer";
    public const string CreditCard = "credit_card";
    public const string LinePay = "line_pay";
    public const string Other = "other";
}

public static class PaymentTypes
{
    public const string SingleSession = "single_session";
    public const string PackagePurchase = "package_purchase";
}

public static class PaymentStatus
{
    public const string Paid = "paid";
    public const string Pending = "pending";
    public const string Refunded = "refunded";
    public const string Waived = "waived";
}
