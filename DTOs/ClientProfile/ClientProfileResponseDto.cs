using System.Text.Json.Serialization;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.ClientProfile;

public class ClientProfileResponseDto
{
    public int Id { get; set; }
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
    [ConstantValues(typeof(ClientStatus))]
    public string Status { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public bool MustChangePassword { get; set; }

    /// <summary>只在建立附帳號的個案時回傳一次；明文不保存，其他回應不含此欄位。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InitialCredentialsDto? InitialCredentials { get; set; }
    public List<TherapistInfoDto> Therapists { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TherapistInfoDto
{
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    [ConstantValues(typeof(TherapistClientStatus))]
    public string Status { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

public class InitialCredentialsDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
