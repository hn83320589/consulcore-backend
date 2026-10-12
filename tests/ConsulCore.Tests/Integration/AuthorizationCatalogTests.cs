using System.Reflection;
using consulcore_backend.Authorization;
using consulcore_backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsulCore.Tests.Integration;

// R3：角色 / 權限碼 / policy 名稱原本以字串散落在 Program、Seeder、AuthService 與 28 個 controller，
// 打錯字不會編譯失敗；預設角色權限裡的錯字甚至會在開設機構時被默默略過。
public class AuthorizationCatalogTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public void Every_policy_used_by_a_controller_is_registered()
    {
        var used = typeof(Program).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller"))
            .SelectMany(t => t.GetCustomAttributes<AuthorizeAttribute>()
                .Concat(t.GetMethods().SelectMany(m => m.GetCustomAttributes<AuthorizeAttribute>())))
            .Select(a => a.Policy)
            .Where(p => p != null)
            .Distinct()
            .ToList();

        Assert.NotEmpty(used);
        Assert.All(used, p => Assert.Contains(p!, Policies.All));
    }

    [Fact]
    public void Every_policy_maps_to_a_known_permission()
    {
        var codes = Policies.PermissionByPolicy.Values.Concat(Policies.AnyPermissionByPolicy.Values.SelectMany(c => c));
        Assert.All(codes, code => Assert.Contains(code, Permissions.All));
    }

    [Fact]
    public async Task Seeded_permissions_match_the_catalog_exactly()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seeded = await db.Permissions.Select(p => p.Code).ToListAsync();

        Assert.Equal(Permissions.All.OrderBy(c => c), seeded.OrderBy(c => c));
    }

    [Fact]
    public async Task New_organization_roles_get_exactly_the_default_permissions()
    {
        await _api.RegisterOrganizationAsync("catalog 診所", "catalog_owner");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var granted = await db.Roles.IgnoreQueryFilters()
            .Where(r => r.Organization.Name == "catalog 診所")
            .Select(r => new { r.Name, Codes = r.RolePermissions.Select(rp => rp.Permission.Code).ToList() })
            .ToListAsync();

        Assert.Equal(DefaultRolePermissions.ByRole.Keys.OrderBy(k => k), granted.Select(g => g.Name).OrderBy(n => n));
        Assert.All(granted, g => Assert.Equal(
            DefaultRolePermissions.ByRole[g.Name].OrderBy(c => c), g.Codes.OrderBy(c => c)));
    }
}
