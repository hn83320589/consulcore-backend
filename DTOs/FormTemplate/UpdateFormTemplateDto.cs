using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.FormTemplate;

public class UpdateFormTemplateDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>
    /// 完整覆蓋 sections/fields，version++
    /// </summary>
    public List<CreateFormSectionDto>? Sections { get; set; }
}
