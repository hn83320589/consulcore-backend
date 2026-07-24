namespace consulcore_backend.DTOs.PhraseTemplate;

public class UpdatePhraseTemplateDto
{
    public string? Category { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int? SortOrder { get; set; }
}
