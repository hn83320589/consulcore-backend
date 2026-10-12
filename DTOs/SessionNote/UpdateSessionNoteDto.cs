namespace consulcore_backend.DTOs.SessionNote;

/// <summary>只更新內容，不含 type / format</summary>
public class UpdateSessionNoteDto
{
    public string? SoapS { get; set; }
    public string? SoapO { get; set; }
    public string? SoapA { get; set; }
    public string? SoapP { get; set; }
    public string? FreeTextContent { get; set; }
}
