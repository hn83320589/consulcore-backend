namespace consulcore_backend.DTOs.ClientProfile;

public class UpdateClientProfileDto
{
    public string? Name { get; set; }
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
}
