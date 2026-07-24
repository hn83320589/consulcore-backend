using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.Discount;

public class CreateDiscountDto
{
    [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(20)] public string DiscountType { get; set; } = "percentage";
    public decimal? PercentageValue { get; set; }
    public decimal? FixedAmount { get; set; }
    [MaxLength(50)] public string? ApplicableRole { get; set; }
    // null = 機構通用折扣；指定心理師 = 專屬折扣
    public int? TherapistUserId { get; set; }
}

public class UpdateDiscountDto
{
    public string? Name { get; set; }
    public decimal? PercentageValue { get; set; }
    public decimal? FixedAmount { get; set; }
    public bool? IsActive { get; set; }
}

public class DiscountResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal? PercentageValue { get; set; }
    public decimal? FixedAmount { get; set; }
    public string? ApplicableRole { get; set; }
    public int? TherapistUserId { get; set; }
    public bool IsActive { get; set; }
}

public class AssignDiscountDto
{
    public int ClientProfileId { get; set; }
    public int DiscountId { get; set; }
    public string? Note { get; set; }
}
