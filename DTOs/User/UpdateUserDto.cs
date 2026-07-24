using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.User;

public class UpdateUserDto
{
    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? DisplayName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public List<string>? Roles { get; set; }
}
