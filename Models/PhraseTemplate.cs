using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("phrase_templates")]
public class PhraseTemplate
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("therapist_user_id")]
    public int TherapistUserId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("category")]
    public string Category { get; set; } = PhraseCategory.General;

    [Required]
    [MaxLength(100)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("TherapistUserId")]
    public User TherapistUser { get; set; } = null!;
}

public static class PhraseCategory
{
    public const string Subjective = "subjective";
    public const string Objective = "objective";
    public const string Assessment = "assessment";
    public const string Plan = "plan";
    public const string General = "general";
}
