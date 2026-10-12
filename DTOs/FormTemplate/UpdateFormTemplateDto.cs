using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.FormTemplate;

public class UpdateFormTemplateDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>傳入時整組覆蓋並 version +1；null 表示不變更</summary>
    public List<CreateFormSectionDto>? Sections { get; set; }
}
