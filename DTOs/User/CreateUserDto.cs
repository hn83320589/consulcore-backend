using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.User;

public class CreateUserDto
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>角色名稱，至少 1 個</summary>
    [Required]
    [MinLength(1)]
    public List<string> Roles { get; set; } = [];
}
