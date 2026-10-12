using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.ConsentRecord;

public class ConsentRecordResponseDto
{
    public int Id { get; set; }
    public int ConsentTemplateId { get; set; }
    public string TemplateTitle { get; set; } = string.Empty;
    public int ClientProfileId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ContentSnapshot { get; set; } = string.Empty;
    public int TemplateVersion { get; set; }
    public string? SignatureImage { get; set; }
    public bool SignedRemotely { get; set; }
    [ConstantValues(typeof(ConsentRecordStatus))]
    public string Status { get; set; } = string.Empty;
    public DateTime? SignedAt { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
