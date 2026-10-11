using System.Security.Claims;
using System.Text.Json;

namespace consulcore_backend.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) => GetIntClaim(user, JwtClaims.UserId);

    public static int GetOrganizationId(this ClaimsPrincipal user) => GetIntClaim(user, JwtClaims.OrganizationId);

    /// <summary>JWT roles claim（JSON 陣列字串）解析為角色清單；缺少或格式錯誤時回空清單（fail-closed）。</summary>
    public static IReadOnlyList<string> GetJwtRoles(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(JwtClaims.Roles)?.Value;
        if (string.IsNullOrEmpty(claim)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(claim) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    // 只用在已通過 [Authorize] 的請求：缺少 claim 代表 token 結構有問題，屬伺服器端錯誤
    private static int GetIntClaim(ClaimsPrincipal user, string type)
    {
        var value = user.FindFirst(type)?.Value;
        if (!int.TryParse(value, out var id))
            throw new InvalidOperationException($"token 缺少有效的 {type} claim");
        return id;
    }
}
