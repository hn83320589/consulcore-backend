using System.Security.Claims;

namespace consulcore_backend.Authorization;

/// <summary>
/// 依 X-Active-Role header + JWT roles claim 判斷請求的「當前身份範圍」。
///
/// X-Active-Role 是 client 可控 header，單看 header 不可信；一律需對照 JWT 的
/// roles claim 驗證（fail-closed）。與 <c>PermissionHandler</c>、
/// <c>ClientProfilesController.HasFullClientScope</c> 同一套原則：
/// header 偽造只會讓存取「更受限」，永遠不會「更寬鬆」。
/// </summary>
public static class ActiveRoleScope
{
    /// <summary>
    /// 請求是否以「已驗證的非個案（staff）角色」發出。
    /// active role 必須非 <c>client</c>、且確實存在於 JWT roles 才承認。
    /// 只有 true 時才可放行跨個案存取；其餘（含偽造 header、無 header、個案本人）皆需限縮為本人。
    /// </summary>
    public static bool ActingAsVerifiedNonClient(HttpRequest request, ClaimsPrincipal user)
    {
        var role = request.Headers["X-Active-Role"].FirstOrDefault();
        if (string.IsNullOrEmpty(role) || role == Roles.Client) return false;
        return user.GetJwtRoles().Contains(role);
    }

    /// <summary>請求是否以指定角色發出，且該角色確實存在於 JWT roles。</summary>
    public static bool ActingAsVerified(HttpRequest request, ClaimsPrincipal user, string role)
    {
        return request.Headers["X-Active-Role"].FirstOrDefault() == role
            && user.GetJwtRoles().Contains(role);
    }
}
