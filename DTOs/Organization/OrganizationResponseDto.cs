namespace consulcore_backend.DTOs.Organization;

/// <summary>機構基本資料（機構設定頁）</summary>
public class OrganizationResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
