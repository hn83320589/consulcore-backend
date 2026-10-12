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
        if (!context.User.GetJwtRoles().Contains(activeRole)) return;

        var orgIdClaim = context.User.FindFirst(JwtClaims.OrganizationId)?.Value;
        if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var hasPermission = await db.Roles
            .IgnoreQueryFilters()
            .Where(r => r.OrganizationId == orgId && r.Name == activeRole)
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission)
            .AnyAsync(p => requirement.PermissionCodes.Contains(p.Code));

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
}
