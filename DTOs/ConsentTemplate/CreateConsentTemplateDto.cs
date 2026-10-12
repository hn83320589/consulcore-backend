using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.ConsentTemplate;

public class CreateConsentTemplateDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(50)]
    [ConstantValues(typeof(ConsentTriggerEvent))]
    public string TriggerEvent { get; set; } = "manual";
}
