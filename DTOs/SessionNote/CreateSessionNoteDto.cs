using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.SessionNote;

public class CreateSessionNoteDto
{
    [Required]
    public int ClientProfileId { get; set; }

    public int? AppointmentId { get; set; }

    [MaxLength(20)]
    [ConstantValues(typeof(SessionNoteType))]
    public string Type { get; set; } = "session";

    [MaxLength(20)]
    [ConstantValues(typeof(SessionNoteFormat))]
    public string Format { get; set; } = "soap";

    public string? SoapS { get; set; }
    public string? SoapO { get; set; }
    public string? SoapA { get; set; }
    public string? SoapP { get; set; }
    public string? FreeTextContent { get; set; }
}
