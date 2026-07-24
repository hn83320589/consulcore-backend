namespace consulcore_backend.DTOs.ClientMemo;

public class ClientMemoResponseDto
{
    public int Id { get; set; }
    public int ClientProfileId { get; set; }
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsEditable { get; set; } // true = 當前心理師, false = 他人
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
