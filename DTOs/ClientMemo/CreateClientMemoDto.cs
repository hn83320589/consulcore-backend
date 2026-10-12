using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.ClientMemo;

public class CreateClientMemoDto
{
    [Required]
    public int ClientProfileId { get; set; }

    [Required]
    [MaxLength(30)]
    [ConstantValues(typeof(MemoCategory))]
    public string Category { get; set; } = "other";

    [Required]
    public string Content { get; set; } = string.Empty;
}
