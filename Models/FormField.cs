using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("form_fields")]
public class FormField
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("section_id")]
    public int SectionId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("label")]
    public string Label { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [Column("field_type")]
    public string FieldType { get; set; } = FieldTypes.Text;

    [Column("options")]
    public string? Options { get; set; } // JSON array: ["option1","option2"]

    [Column("is_required")]
    public bool IsRequired { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [MaxLength(200)]
    [Column("placeholder")]
    public string? Placeholder { get; set; }

    [Column("validation_rules")]
    public string? ValidationRules { get; set; } // JSON: {"min":0,"max":10}

    // Navigation
    [ForeignKey("SectionId")]
    public FormSection Section { get; set; } = null!;
}

public static class FieldTypes
{
    public const string Text = "text";
    public const string Textarea = "textarea";
    public const string Select = "select";
    public const string Radio = "radio";
    public const string Checkbox = "checkbox";
    public const string Date = "date";
    public const string Number = "number";
    public const string Scale = "scale";
}
