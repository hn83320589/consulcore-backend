namespace consulcore_backend.DTOs.Role;

public class RoleResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemDefault { get; set; }
    public List<string> Permissions { get; set; } = [];
}
