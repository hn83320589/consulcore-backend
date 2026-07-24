using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.Role;

public class UpdateRolePermissionsDto
{
    [Required]
    public List<string> PermissionCodes { get; set; } = [];
}
