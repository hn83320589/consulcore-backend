using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using consulcore_backend.Data.Encryption;

namespace consulcore_backend.Models;

[Table("form_field_values")]
public class FormFieldValue
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("submission_id")]
    public int SubmissionId { get; set; }

    [Column("field_id")]
    public int FieldId { get; set; }

    [Encrypted]
    [Column("value")]
    public string? Value { get; set; } // JSON string, encrypted

    // Navigation
    [ForeignKey("SubmissionId")]
    public FormSubmission Submission { get; set; } = null!;

    [ForeignKey("FieldId")]
    public FormField Field { get; set; } = null!;
}
