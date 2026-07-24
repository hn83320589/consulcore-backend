using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.ClientProfile;

public class CreateClientProfileDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? IdNumber { get; set; }
    public string? Gender { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? Occupation { get; set; }
    public string? ReferralSource { get; set; }
    public string? Notes { get; set; }
    public string? MedicalHistory { get; set; }

    /// <summary>
    /// true = 建立帳號（預設密碼：身分證後4碼+生日MMDD）
    /// false = 不建立帳號
    /// </summary>
    public bool CreateAccount { get; set; }

    /// <summary>
    /// 指派的心理師 UserId（可選）
    /// </summary>
    public int? TherapistUserId { get; set; }
}
