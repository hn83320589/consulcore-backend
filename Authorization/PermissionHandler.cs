using System.Text.Json;
using consulcore_backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PermissionHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var activeRole = GetActiveRole(context);
        if (activeRole == null) return;

        // 防止 X-Active-Role header 偽造提權：active role 必須是使用者 JWT 內確實擁有的角色
        if (!GetUserRoles(context).Contains(activeRole)) return;

        var orgIdClaim = context.User.FindFirst("organizationId")?.Value;
        if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var hasPermission = await db.Roles
            .IgnoreQueryFilters()
            .Where(r => r.OrganizationId == orgId && r.Name == activeRole)
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission)
            .AnyAsync(p => p.Code == requirement.PermissionCode);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }

    private static string? GetActiveRole(AuthorizationHandlerContext context)
    {
        if (context.Resource is HttpContext httpContext)
        {
            var headerRole = httpContext.Request.Headers["X-Active-Role"].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerRole)) return headerRole;
        }

        return null;
    }

    // 解析 JWT roles claim（JSON 陣列字串），取得使用者實際擁有的角色集合
    private static List<string> GetUserRoles(AuthorizationHandlerContext context)
    {
        var rolesClaim = context.User.FindFirst("roles")?.Value;
        if (string.IsNullOrEmpty(rolesClaim)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(rolesClaim) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
