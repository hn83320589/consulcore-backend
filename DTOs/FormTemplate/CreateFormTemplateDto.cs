using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.FormTemplate;

public class CreateFormTemplateDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Type { get; set; } = "intake_assessment";

    public List<CreateFormSectionDto> Sections { get; set; } = [];
}

public class CreateFormSectionDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool IsRequired { get; set; } = true;

    public List<CreateFormFieldDto> Fields { get; set; } = [];
}

public class CreateFormFieldDto
{
    [Required]
    [MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(30)]
    public string FieldType { get; set; } = "text";

    public string? Options { get; set; } // JSON
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }

    [MaxLength(200)]
    public string? Placeholder { get; set; }

    public string? ValidationRules { get; set; } // JSON
}
