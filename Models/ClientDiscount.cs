using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("client_discounts")]
public class ClientDiscount
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("discount_id")]
    public int DiscountId { get; set; }

    [Column("assigned_by")]
    public int AssignedBy { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("ClientProfileId")]
    public ClientProfile ClientProfile { get; set; } = null!;

    [ForeignKey("DiscountId")]
    public Discount Discount { get; set; } = null!;
}
