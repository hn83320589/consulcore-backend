using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.FormTemplate;

public class FormTemplateResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [ConstantValues(typeof(FormTemplateType))]
    public string Type { get; set; } = string.Empty;
    public bool IsSystemDefault { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public List<FormSectionResponseDto> Sections { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FormSectionResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public List<FormFieldResponseDto> Fields { get; set; } = [];
}

public class FormFieldResponseDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    [ConstantValues(typeof(FieldTypes))]
    public string FieldType { get; set; } = string.Empty;
    public string? Options { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public string? Placeholder { get; set; }
    public string? ValidationRules { get; set; }
}
