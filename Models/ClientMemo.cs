using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using consulcore_backend.Data.Encryption;

namespace consulcore_backend.Models;

[Table("client_memos")]
public class ClientMemo
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

    [Required]
    [MaxLength(30)]
    [Column("category")]
    public string Category { get; set; } = MemoCategory.Other;

    [Encrypted]
    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

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
}

public static class MemoCategory
{
    public const string Personality = "personality";
    public const string Trigger = "trigger";
    public const string Preference = "preference";
    public const string Family = "family";
    public const string Other = "other";
}
