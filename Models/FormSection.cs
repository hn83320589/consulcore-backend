using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("form_sections")]
public class FormSection
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_required")]
    public bool IsRequired { get; set; } = true;

    // Navigation
    [ForeignKey("TemplateId")]
    public FormTemplate Template { get; set; } = null!;

    public ICollection<FormField> Fields { get; set; } = [];
}
