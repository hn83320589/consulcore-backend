using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("form_submissions")]
public class FormSubmission
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("template_version")]
    public int TemplateVersion { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("therapist_user_id")]
    public int TherapistUserId { get; set; }

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [Column("is_encrypted")]
    public bool IsEncrypted { get; set; } = true;

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("TemplateId")]
    public FormTemplate Template { get; set; } = null!;

    [ForeignKey("ClientProfileId")]
    public ClientProfile ClientProfile { get; set; } = null!;

    [ForeignKey("TherapistUserId")]
    public User TherapistUser { get; set; } = null!;

    public ICollection<FormFieldValue> FieldValues { get; set; } = [];
}
