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
    public string Status { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public bool MustChangePassword { get; set; }
    public List<TherapistInfoDto> Therapists { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TherapistInfoDto
{
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}
