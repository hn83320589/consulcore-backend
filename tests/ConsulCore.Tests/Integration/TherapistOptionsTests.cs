using System.Net;
using System.Text.Json;

namespace ConsulCore.Tests.Integration;

// 排班、預約、考核頁的心理師下拉選單：/api/users 需要 staff.manage 且含 Email、電話，
// 這支只給 id 與姓名，有任一頁的檢視權限就能用
public class TherapistOptionsTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<JsonElement> GetOptionsAsync(string token, string role) =>
        await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, "/api/therapists", token, role));

    private async Task SetTherapistPermissionsAsync(string ownerToken, params string[] codes)
    {
        var roles = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, "/api/roles", ownerToken, "owner"));
        var therapistRoleId = roles.EnumerateArray()
            .First(r => r.GetProperty("name").GetString() == "therapist").GetProperty("id").GetInt32();
        await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Put, $"/api/roles/{therapistRoleId}/permissions",
            ownerToken, "owner", new { permissionCodes = codes }));
    }

    [Fact]
    public async Task Lists_only_active_therapists_of_the_organization_with_id_and_name()
    {
        var owner = await _api.RegisterOrganizationAsync("opt1 診所", "opt1_owner");
        var alice = await _api.CreateUserAsync(owner, "opt1_alice", "therapist");
        var retired = await _api.CreateUserAsync(owner, "opt1_retired", "therapist");
        await _api.CreateUserAsync(owner, "opt1_admin", "admin");
        await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Patch,
            $"/api/users/{retired.GetProperty("id").GetInt32()}/toggle-active", owner, "owner"));
        var otherOrg = await _api.RegisterOrganizationAsync("opt1 他所", "opt1_other");
        await _api.CreateUserAsync(otherOrg, "opt1_outsider", "therapist");

        var options = await GetOptionsAsync(owner, "owner");

        var only = Assert.Single(options.EnumerateArray());
        Assert.Equal(alice.GetProperty("id").GetInt32(), only.GetProperty("id").GetInt32());
        Assert.Equal("opt1_alice", only.GetProperty("displayName").GetString());
        Assert.Equal(["displayName", "id"], only.EnumerateObject().Select(p => p.Name).Order());
    }

    [Fact]
    public async Task Any_one_of_schedule_appointment_or_performance_view_is_enough()
    {
        var owner = await _api.RegisterOrganizationAsync("opt2 診所", "opt2_owner");
        await _api.CreateUserAsync(owner, "opt2_ther", "therapist");
        await SetTherapistPermissionsAsync(owner, "schedule.view");
        var therapist = await _api.LoginTokenAsync("opt2_ther", "Pass123!");

        var options = await GetOptionsAsync(therapist, "therapist");

        Assert.Single(options.EnumerateArray());
    }

    [Fact]
    public async Task Role_without_any_of_those_permissions_is_forbidden()
    {
        var owner = await _api.RegisterOrganizationAsync("opt3 診所", "opt3_owner");
        await _api.CreateUserAsync(owner, "opt3_ther", "therapist");
        await SetTherapistPermissionsAsync(owner, "session.view");
        var therapist = await _api.LoginTokenAsync("opt3_ther", "Pass123!");

        var response = await _api.SendAsync(HttpMethod.Get, "/api/therapists", therapist, "therapist");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
