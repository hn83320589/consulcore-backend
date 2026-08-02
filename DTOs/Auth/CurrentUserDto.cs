namespace consulcore_backend.DTOs.Auth;

public class CurrentUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public string? ActiveRole { get; set; }
    public List<string> Permissions { get; set; } = [];
    // 若此帳號為個案（有對應 ClientProfile），提供其 clientProfileId 供個案端查詢自己的資料
    public int? ClientProfileId { get; set; }
}
