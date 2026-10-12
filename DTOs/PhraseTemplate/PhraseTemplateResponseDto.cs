using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.PhraseTemplate;

public class PhraseTemplateResponseDto
{
    public int Id { get; set; }
    [ConstantValues(typeof(PhraseCategory))]
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
