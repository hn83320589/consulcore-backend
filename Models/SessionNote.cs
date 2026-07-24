using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using consulcore_backend.Data.Encryption;

namespace consulcore_backend.Models;

[Table("session_notes")]
public class SessionNote
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("therapist_user_id")]
    public int TherapistUserId { get; set; }

    [Column("appointment_id")]
    public int? AppointmentId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("type")]
    public string Type { get; set; } = SessionNoteType.Session;

    [Required]
    [MaxLength(20)]
    [Column("format")]
    public string Format { get; set; } = SessionNoteFormat.Soap;

    [Encrypted]
    [Column("soap_s")]
    public string? SoapS { get; set; }

    [Encrypted]
    [Column("soap_o")]
    public string? SoapO { get; set; }

    [Encrypted]
    [Column("soap_a")]
    public string? SoapA { get; set; }

    [Encrypted]
    [Column("soap_p")]
    public string? SoapP { get; set; }

    [Encrypted]
    [Column("free_text_content")]
    public string? FreeTextContent { get; set; }

    [Column("intake_summary_imported")]
    public bool IntakeSummaryImported { get; set; }

    [Column("is_locked")]
    public bool IsLocked { get; set; }

    [Column("locked_at")]
    public DateTime? LockedAt { get; set; }

    [Column("locked_by")]
    public int? LockedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("ClientProfileId")]
    public ClientProfile ClientProfile { get; set; } = null!;

    [ForeignKey("TherapistUserId")]
    public User TherapistUser { get; set; } = null!;

    [ForeignKey("AppointmentId")]
    public Appointment? Appointment { get; set; }
}

public static class SessionNoteType
{
    public const string Session = "session";
    public const string Closure = "closure";
}

public static class SessionNoteFormat
{
    public const string Soap = "soap";
    public const string FreeText = "free_text";
}
