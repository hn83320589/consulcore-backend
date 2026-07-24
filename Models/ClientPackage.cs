using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("client_packages")]
public class ClientPackage
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("package_id")]
    public int PackageId { get; set; }

    [Column("purchased_at")]
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("sessions_used")]
    public int SessionsUsed { get; set; }

    [Column("sessions_remaining")]
    public int SessionsRemaining { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = ClientPackageStatus.Active;

    [Column("payment_record_id")]
    public int? PaymentRecordId { get; set; }

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("ClientProfileId")]
    public ClientProfile ClientProfile { get; set; } = null!;

    [ForeignKey("PackageId")]
    public Package Package { get; set; } = null!;

    [ForeignKey("PaymentRecordId")]
    public PaymentRecord? PaymentRecord { get; set; }
}

public static class ClientPackageStatus
{
    public const string Active = "active";
    public const string Expired = "expired";
    public const string Completed = "completed";
}
