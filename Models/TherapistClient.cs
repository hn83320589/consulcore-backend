using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("therapist_clients")]
public class TherapistClient
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("therapist_user_id")]
    public int TherapistUserId { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = TherapistClientStatus.Active;

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [MaxLength(500)]
    [Column("ended_reason")]
    public string? EndedReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("TherapistUserId")]
    public User TherapistUser { get; set; } = null!;

    [ForeignKey("ClientProfileId")]
    public ClientProfile ClientProfile { get; set; } = null!;
}

public static class TherapistClientStatus
{
    public const string Active = "active";
    public const string Transferred = "transferred";
    public const string Closed = "closed";
}
