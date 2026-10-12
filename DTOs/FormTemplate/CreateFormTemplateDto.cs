using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.FormTemplate;

public class CreateFormTemplateDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    [ConstantValues(typeof(FormTemplateType))]
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
    [ConstantValues(typeof(FieldTypes))]
    public string FieldType { get; set; } = "text";

    /// <summary>JSON 字串（select / radio / checkbox 的選項）</summary>
    public string? Options { get; set; } // JSON
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }

    [MaxLength(200)]
    public string? Placeholder { get; set; }

    /// <summary>JSON 字串</summary>
    public string? ValidationRules { get; set; } // JSON
}
