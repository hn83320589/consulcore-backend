using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.ConsentRecord;

public class SignConsentDto
{
    [Required]
    public int ConsentTemplateId { get; set; }

    [Required]
    public int ClientProfileId { get; set; }

    /// <summary>
    /// Base64 encoded signature image
    /// </summary>
    public string? SignatureImage { get; set; }

    public bool SignedRemotely { get; set; }
}

public class VoidConsentDto
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}
