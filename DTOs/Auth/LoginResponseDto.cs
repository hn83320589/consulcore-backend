namespace consulcore_backend.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>個案帳號建立時沒有提供 Email 則為 null</summary>
    public string? Email { get; set; }
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public bool MustChangePassword { get; set; }
}
