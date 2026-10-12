using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.SessionNote;

public class SessionNoteResponseDto
{
    public int Id { get; set; }
    public int ClientProfileId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
    [ConstantValues(typeof(SessionNoteType))]
    public string Type { get; set; } = string.Empty;
    [ConstantValues(typeof(SessionNoteFormat))]
    public string Format { get; set; } = string.Empty;
    public string? SoapS { get; set; }
    public string? SoapO { get; set; }
    public string? SoapA { get; set; }
    public string? SoapP { get; set; }
    public string? FreeTextContent { get; set; }
    public bool IntakeSummaryImported { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public int? LockedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
