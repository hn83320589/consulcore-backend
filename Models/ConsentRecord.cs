using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using consulcore_backend.Data.Encryption;

namespace consulcore_backend.Models;

[Table("consent_records")]
public class ConsentRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("consent_template_id")]
    public int ConsentTemplateId { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Encrypted]
    [Required]
    [Column("content_snapshot")]
    public string ContentSnapshot { get; set; } = string.Empty;

    [Column("template_version")]
    public int TemplateVersion { get; set; }

    [Encrypted]
    [Column("signature_image")]
    public string? SignatureImage { get; set; }

    [Column("signed_remotely")]
    public bool SignedRemotely { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = ConsentRecordStatus.Pending;

    [Column("signed_at")]
    public DateTime? SignedAt { get; set; }

    [Column("voided_at")]
    public DateTime? VoidedAt { get; set; }

    [MaxLength(500)]
    [Column("void_reason")]
    public string? VoidReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("ConsentTemplateId")]
    public ConsentTemplate ConsentTemplate { get; set; } = null!;

    [ForeignKey("ClientProfileId")]
    public ClientProfile ClientProfile { get; set; } = null!;
}

public static class ConsentRecordStatus
{
    public const string Pending = "pending";
    public const string Signed = "signed";
    public const string Voided = "voided";
}
