using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using consulcore_backend.Data.Encryption;

namespace consulcore_backend.Models;

[Table("violation_records")]
public class ViolationRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("therapist_user_id")]
    public int TherapistUserId { get; set; }

    [Column("reported_by")]
    public int ReportedBy { get; set; }

    [Column("incident_date")]
    public DateOnly IncidentDate { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("severity")]
    public string Severity { get; set; } = ViolationSeverity.Low;

    [Encrypted]
    [Required]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Encrypted]
    [Column("action_taken")]
    public string? ActionTaken { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = ViolationStatus.Open;

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("TherapistUserId")]
    public User TherapistUser { get; set; } = null!;
}

public static class ViolationSeverity
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";
}

public static class ViolationStatus
{
    public const string Open = "open";
    public const string Resolved = "resolved";
    public const string Dismissed = "dismissed";
}
