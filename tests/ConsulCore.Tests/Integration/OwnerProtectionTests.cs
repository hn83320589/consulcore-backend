using System.Net;
using System.Text.Json;

namespace ConsulCore.Tests.Integration;

// B2：admin 有 staff.manage，但不可藉此升級為 owner 或接管 owner 帳號
public class OwnerProtectionTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private record Org(string Owner, string Admin, int OwnerUserId, int TherapistUserId);

    private async Task<Org> SetUpAsync(string prefix)
    {
        var owner = await _api.RegisterOrganizationAsync($"{prefix} 診所", $"{prefix}_owner");
        await _api.CreateUserAsync(owner, $"{prefix}_admin", "admin");
        var therapist = await _api.CreateUserAsync(owner, $"{prefix}_ther", "therapist");
        var admin = await _api.LoginTokenAsync($"{prefix}_admin", "Pass123!");
        var users = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, "/api/users", owner, "owner"));
        var ownerUserId = users.EnumerateArray()
            .First(u => u.GetProperty("roles").EnumerateArray().Any(r => r.GetString() == "owner"))
            .GetProperty("id").GetInt32();
        return new Org(owner, admin, ownerUserId, therapist.GetProperty("id").GetInt32());
    }

    private static object NewUser(string username, string role) => new
    {
        username,
        email = $"{username}@test.local",
        password = "Pass123!",
        displayName = username,
        roles = new[] { role },
    };

    private async Task<int> RoleIdAsync(string token, string roleName)
    {
        var roles = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, "/api/roles", token, "owner"));
        return roles.EnumerateArray().First(r => r.GetProperty("name").GetString() == roleName).GetProperty("id").GetInt32();
    }

    private static async Task AssertForbiddenAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.False(body.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task Admin_cannot_create_a_user_with_the_owner_role()
    {
        var org = await SetUpAsync("op1");

        await AssertForbiddenAsync(
            await _api.SendAsync(HttpMethod.Post, "/api/users", org.Admin, "admin", NewUser("op1_new", "owner")));
    }

    [Fact]
    public async Task Admin_cannot_grant_the_owner_role_to_an_existing_user()
    {
        var org = await SetUpAsync("op2");

        await AssertForbiddenAsync(await _api.SendAsync(HttpMethod.Put, $"/api/users/{org.TherapistUserId}",
            org.Admin, "admin", new { roles = new[] { "therapist", "owner" } }));
    }

    [Fact]
    public async Task Admin_cannot_modify_reset_or_deactivate_the_owner_account()
    {
        var org = await SetUpAsync("op3");

        await AssertForbiddenAsync(await _api.SendAsync(HttpMethod.Put, $"/api/users/{org.OwnerUserId}",
            org.Admin, "admin", new { email = "attacker@test.local" }));
        await AssertForbiddenAsync(await _api.SendAsync(HttpMethod.Post, $"/api/users/{org.OwnerUserId}/reset-password",
            org.Admin, "admin", new { newPassword = "Hijacked123!" }));
        await AssertForbiddenAsync(await _api.SendAsync(HttpMethod.Patch, $"/api/users/{org.OwnerUserId}/toggle-active",
            org.Admin, "admin"));
    }

    [Fact]
    public async Task Admin_cannot_change_permissions_of_system_roles()
    {
        var org = await SetUpAsync("op4");
        var adminRoleId = await RoleIdAsync(org.Owner, "admin");

        await AssertForbiddenAsync(await _api.SendAsync(HttpMethod.Put, $"/api/roles/{adminRoleId}/permissions",
            org.Admin, "admin", new { permissionCodes = new[] { "staff.manage", "org.settings", "audit.view" } }));
    }

    [Fact]
    public async Task Admin_can_still_manage_non_owner_staff()
    {
        var org = await SetUpAsync("op5");

        var create = await _api.SendAsync(HttpMethod.Post, "/api/users", org.Admin, "admin", NewUser("op5_new", "therapist"));
        var reset = await _api.SendAsync(HttpMethod.Post, $"/api/users/{org.TherapistUserId}/reset-password",
            org.Admin, "admin", new { newPassword = "Reset123!" });

        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
    }

    [Fact]
    public async Task Owner_can_grant_owner_role_and_change_system_role_permissions()
    {
        var org = await SetUpAsync("op6");
        var adminRoleId = await RoleIdAsync(org.Owner, "admin");

        var grant = await _api.SendAsync(HttpMethod.Put, $"/api/users/{org.TherapistUserId}",
            org.Owner, "owner", new { roles = new[] { "therapist", "owner" } });
        var permissions = await _api.SendAsync(HttpMethod.Put, $"/api/roles/{adminRoleId}/permissions",
            org.Owner, "owner", new { permissionCodes = new[] { "staff.manage", "client.view" } });

        Assert.Equal(HttpStatusCode.OK, grant.StatusCode);
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
    }
}
