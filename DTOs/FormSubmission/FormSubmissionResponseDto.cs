using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.FormSubmission;

public class FormSubmissionResponseDto
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public int TemplateVersion { get; set; }
    public int ClientProfileId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public List<FieldValueResponseDto> Values { get; set; } = [];
}

public class FieldValueResponseDto
{
    public int FieldId { get; set; }
    public string FieldLabel { get; set; } = string.Empty;
    [ConstantValues(typeof(FieldTypes))]
    public string FieldType { get; set; } = string.Empty;
    public string? Value { get; set; }
}
