using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.ViolationRecord;

public class CreateViolationRecordDto
{
    [Required] public int TherapistUserId { get; set; }
    [Required] public string IncidentDate { get; set; } = string.Empty;
    [MaxLength(20)] public string Severity { get; set; } = "low";
    [Required] public string Description { get; set; } = string.Empty;
    public string? ActionTaken { get; set; }
}

public class UpdateViolationRecordDto
{
    public string? Severity { get; set; }
    public string? Description { get; set; }
    public string? ActionTaken { get; set; }
}

public class ResolveViolationDto
{
    [Required] public string ActionTaken { get; set; } = string.Empty;
}

public class ViolationRecordResponseDto
{
    public int Id { get; set; }
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public int ReportedBy { get; set; }
    public string IncidentDate { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ActionTaken { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
