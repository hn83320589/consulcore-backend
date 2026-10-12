using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.ClientMemo;

public class ClientMemoResponseDto
{
    public int Id { get; set; }
    public int ClientProfileId { get; set; }
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    [ConstantValues(typeof(MemoCategory))]
    public string Category { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    /// <summary>true = 目前的心理師可編輯；false = 他人所寫，唯讀（BR-16）</summary>
    public bool IsEditable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
