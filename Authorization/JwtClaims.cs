namespace consulcore_backend.Authorization;

/// <summary>JWT 內自訂 claim 的名稱（AuthService 產生 token 與各處讀取共用）。</summary>
public static class JwtClaims
{
    public const string UserId = "userId";
    public const string OrganizationId = "organizationId";
    public const string Username = "username";
    public const string Roles = "roles"; // JSON 陣列字串，例如 ["therapist","client"]
    public const string MustChangePassword = "mustChangePassword";
}
