using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.Discount;

public class CreateDiscountDto
{
    [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [ConstantValues(typeof(DiscountTypes))]
    [MaxLength(20)] public string DiscountType { get; set; } = "percentage";
    public decimal? PercentageValue { get; set; }
    public decimal? FixedAmount { get; set; }
    /// <summary>綁定角色標籤時自動套用（BR-11）</summary>
    [MaxLength(50)] public string? ApplicableRole { get; set; }
    /// <summary>null = 機構通用折扣；有值 = 該心理師專屬（查找時專屬優先）</summary>
    public int? TherapistUserId { get; set; }
}

/// <summary>不含 TherapistUserId：心理師綁定只在建立時設定</summary>
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
    [ConstantValues(typeof(DiscountTypes))]
    public string DiscountType { get; set; } = string.Empty;
    public decimal? PercentageValue { get; set; }
    public decimal? FixedAmount { get; set; }
    /// <summary>綁定角色標籤時自動套用（BR-11）</summary>
    public string? ApplicableRole { get; set; }
    /// <summary>null = 機構通用折扣；有值 = 該心理師專屬（查找時專屬優先）</summary>
    public int? TherapistUserId { get; set; }
    public bool IsActive { get; set; }
}

public class AssignDiscountDto
{
    public int ClientProfileId { get; set; }
    public int DiscountId { get; set; }
    public string? Note { get; set; }
}
