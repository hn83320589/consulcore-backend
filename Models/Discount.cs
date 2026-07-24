using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("discounts")]
public class Discount
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    // 心理師專屬折扣；null = 機構通用（比照 FeeSchedule，查找時專屬優先於通用）
    [Column("therapist_user_id")]
    public int? TherapistUserId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("discount_type")]
    public string DiscountType { get; set; } = DiscountTypes.Percentage;

    [Column("percentage_value", TypeName = "decimal(5,2)")]
    public decimal? PercentageValue { get; set; }

    [Column("fixed_amount", TypeName = "decimal(10,2)")]
    public decimal? FixedAmount { get; set; }

    [MaxLength(50)]
    [Column("applicable_role")]
    public string? ApplicableRole { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("TherapistUserId")]
    public User? TherapistUser { get; set; }
}

public static class DiscountTypes
{
    public const string Percentage = "percentage";
    public const string FixedAmount = "fixed_amount";
}
