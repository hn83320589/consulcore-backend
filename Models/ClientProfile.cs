using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using consulcore_backend.Data.Encryption;

namespace consulcore_backend.Models;

[Table("client_profiles")]
public class ClientProfile
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Encrypted]
    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Encrypted]
    [Column("id_number")]
    public string? IdNumber { get; set; }

    [Encrypted]
    [Column("gender")]
    public string? Gender { get; set; }

    [Encrypted]
    [Column("date_of_birth")]
    public string? DateOfBirth { get; set; }

    [Encrypted]
    [Column("phone")]
    public string? Phone { get; set; }

    [Encrypted]
    [Column("email")]
    public string? Email { get; set; }

    [Encrypted]
    [Column("address")]
    public string? Address { get; set; }

    [Encrypted]
    [Column("emergency_contact_name")]
    public string? EmergencyContactName { get; set; }

    [Encrypted]
    [Column("emergency_contact_phone")]
    public string? EmergencyContactPhone { get; set; }

    [Encrypted]
    [Column("emergency_contact_relationship")]
    public string? EmergencyContactRelationship { get; set; }

    [Encrypted]
    [Column("occupation")]
    public string? Occupation { get; set; }

    [Encrypted]
    [Column("referral_source")]
    public string? ReferralSource { get; set; }

    [Encrypted]
    [Column("notes")]
    public string? Notes { get; set; }

    [Encrypted]
    [Column("medical_history")]
    public string? MedicalHistory { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = ClientStatus.Pending;

    [Column("must_change_password")]
    public bool MustChangePassword { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>軟刪除（D6 / BR-15）：不再出現在清單與查詢，但歷史紀錄保留。列出與查詢時需明確排除</summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("UserId")]
    public User? User { get; set; }

    public ICollection<TherapistClient> TherapistClients { get; set; } = [];
}

public static class ClientStatus
{
    public const string Pending = "pending";
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Closed = "closed";
}
