using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.User;

public class ResetPasswordDto
{
    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}
