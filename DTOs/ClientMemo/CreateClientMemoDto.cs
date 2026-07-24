using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.ClientMemo;

public class CreateClientMemoDto
{
    [Required]
    public int ClientProfileId { get; set; }

    [Required]
    [MaxLength(30)]
    public string Category { get; set; } = "other";

    [Required]
    public string Content { get; set; } = string.Empty;
}
