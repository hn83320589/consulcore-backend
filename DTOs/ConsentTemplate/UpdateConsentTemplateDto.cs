using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.ConsentTemplate;

public class UpdateConsentTemplateDto
{
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>更新內容時版本自動 +1；既有簽署紀錄保留簽署當時的快照</summary>
    public string? Content { get; set; }

    [MaxLength(50)]
    [ConstantValues(typeof(ConsentTriggerEvent))]
    public string? TriggerEvent { get; set; }

    public bool? IsActive { get; set; }
}
