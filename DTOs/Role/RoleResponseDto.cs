namespace consulcore_backend.DTOs.Role;

public class RoleResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemDefault { get; set; }
    /// <summary>權限碼（如 client.manage）</summary>
    public List<string> Permissions { get; set; } = [];
}
