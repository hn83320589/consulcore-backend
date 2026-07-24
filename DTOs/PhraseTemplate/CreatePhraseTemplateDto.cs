using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.PhraseTemplate;

public class CreatePhraseTemplateDto
{
    [Required]
    [MaxLength(30)]
    public string Category { get; set; } = "general";

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
