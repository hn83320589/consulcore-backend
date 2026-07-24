using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using consulcore_backend.Data.Encryption;

namespace consulcore_backend.Models;

[Table("consent_templates")]
public class ConsentTemplate
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Encrypted]
    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("version")]
    public int Version { get; set; } = 1;

    [Required]
    [MaxLength(50)]
    [Column("trigger_event")]
    public string TriggerEvent { get; set; } = ConsentTriggerEvent.Manual;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    public ICollection<ConsentRecord> ConsentRecords { get; set; } = [];
}

public static class ConsentTriggerEvent
{
    public const string Manual = "manual";
    public const string FirstVisit = "first_visit";
    public const string AnnualRenewal = "annual_renewal";
    public const string ServiceChange = "service_change";
}
