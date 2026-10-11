using System.Net;
using System.Text.Json;
using consulcore_backend.Authorization;
using consulcore_backend.Models;

namespace consulcore_backend.Middleware;

/// <summary>
/// 首次登入強制改密碼（BR：預設密碼登入不改密碼 → API 層攔截所有非改密碼操作）。
/// 當 JWT 帶有 mustChangePassword=true 時，除白名單端點外一律回傳 403。
/// </summary>
public class MustChangePasswordMiddleware
{
    private readonly RequestDelegate _next;

    // 允許在未改密碼狀態下呼叫的端點（改密碼本身、讀取自身資料）；登出為純前端行為，無後端端點
    private static readonly string[] AllowedPaths =
    [
        "/api/auth/change-password",
        "/api/auth/me",
    ];

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true
            && user.FindFirst(JwtClaims.MustChangePassword)?.Value == "true")
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var isAllowed = AllowedPaths.Any(p =>
                path.Equals(p, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                var response = ApiResponse<object>.Fail("首次登入請先變更密碼後才能使用其他功能");
                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await context.Response.WriteAsync(json);
                return;
            }
        }

        await _next(context);
    }
}
