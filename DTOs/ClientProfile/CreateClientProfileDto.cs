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

    /// <summary>true = 同時建立個案帳號；隨機初始帳密只在回應的 initialCredentials 回傳一次</summary>
    public bool CreateAccount { get; set; }

    /// <summary>建檔時指派的心理師（選填；須為本機構啟用中的 therapist）</summary>
    public int? TherapistUserId { get; set; }
}
