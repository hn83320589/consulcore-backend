using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.PhraseTemplate;

public class UpdatePhraseTemplateDto
{
    [ConstantValues(typeof(PhraseCategory))]
    public string? Category { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int? SortOrder { get; set; }
}
