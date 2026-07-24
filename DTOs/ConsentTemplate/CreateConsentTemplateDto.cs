using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.ConsentTemplate;

public class CreateConsentTemplateDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(50)]
    public string TriggerEvent { get; set; } = "manual";
}
