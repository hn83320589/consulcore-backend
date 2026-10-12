using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.FormSubmission;

public class SubmitFormDto
{
    [Required]
    public int TemplateId { get; set; }

    [Required]
    public int ClientProfileId { get; set; }

    [Required]
    public List<FieldValueDto> Values { get; set; } = [];
}

public class FieldValueDto
{
    [Required]
    public int FieldId { get; set; }

    /// <summary>JSON 字串</summary>
    public string? Value { get; set; } // JSON string
}
