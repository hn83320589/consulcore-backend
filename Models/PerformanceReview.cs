using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using consulcore_backend.Data.Encryption;

namespace consulcore_backend.Models;

[Table("performance_reviews")]
public class PerformanceReview
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("therapist_user_id")]
    public int TherapistUserId { get; set; }

    [Column("reviewer_id")]
    public int ReviewerId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("period")]
    public string Period { get; set; } = string.Empty; // e.g. "2026-Q1"

    [Column("professional_score")]
    public int ProfessionalScore { get; set; }

    [Column("client_feedback_score")]
    public int ClientFeedbackScore { get; set; }

    [Column("teamwork_score")]
    public int TeamworkScore { get; set; }

    [Column("overall_score")]
    public int OverallScore { get; set; }

    [Encrypted]
    [Column("comment")]
    public string? Comment { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("visibility")]
    public string Visibility { get; set; } = ReviewVisibility.Hidden;

    [Column("visible_from")]
    public DateTime? VisibleFrom { get; set; }

    [Column("visible_until")]
    public DateTime? VisibleUntil { get; set; }

    [Column("opened_by")]
    public int? OpenedBy { get; set; }

    [Column("opened_at")]
    public DateTime? OpenedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("TherapistUserId")]
    public User TherapistUser { get; set; } = null!;

    [ForeignKey("ReviewerId")]
    public User Reviewer { get; set; } = null!;
}

public static class ReviewVisibility
{
    public const string Hidden = "hidden";
    public const string Visible = "visible";
}
