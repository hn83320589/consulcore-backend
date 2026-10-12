using System.Net;

namespace ConsulCore.Tests.Integration;

// 機構設定頁原本只有表單範本；開設機構後名稱、電話、Email、地址都無法修改
public class OrganizationSettingsTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private static object Changes => new
    {
        name = "晴光心理諮商所（信義）",
        phone = "02-2345-6789",
        email = "hello@clinic.example",
        address = "台北市信義區松仁路 100 號",
    };

    [Fact]
    public async Task Owner_reads_the_organization_created_at_registration()
    {
        var owner = await _api.RegisterOrganizationAsync("org1 診所", "org1_owner");

        var org = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, "/api/organization", owner, "owner"));

        Assert.Equal("org1 診所", org.GetProperty("name").GetString());
        Assert.Equal("0900000000", org.GetProperty("phone").GetString());
        Assert.Equal("org1_owner@test.local", org.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Owner_updates_basic_information_and_it_shows_in_the_current_user()
    {
        var owner = await _api.RegisterOrganizationAsync("org2 診所", "org2_owner");

        var updated = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Put, "/api/organization", owner, "owner", Changes));
        var me = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, "/api/auth/me", owner, "owner"));
        var audit = await ApiTestClient.ReadItemsAsync(await _api.SendAsync(HttpMethod.Get,
            "/api/audit-logs?resourceType=organization", owner, "owner"));

        Assert.Equal("晴光心理諮商所（信義）", updated.GetProperty("name").GetString());
        Assert.Equal("台北市信義區松仁路 100 號", updated.GetProperty("address").GetString());
        Assert.Equal("晴光心理諮商所（信義）", me.GetProperty("organizationName").GetString());
        Assert.Contains(audit.EnumerateArray(), a => a.GetProperty("description").GetString() == "更新機構基本資料");
    }

    [Fact]
    public async Task Name_is_required_and_email_must_be_valid()
    {
        var owner = await _api.RegisterOrganizationAsync("org3 診所", "org3_owner");

        var blankName = await _api.SendAsync(HttpMethod.Put, "/api/organization", owner, "owner",
            new { name = " ", phone = (string?)null, email = (string?)null, address = (string?)null });
        var badEmail = await _api.SendAsync(HttpMethod.Put, "/api/organization", owner, "owner",
            new { name = "org3 診所", email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, blankName.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badEmail.StatusCode);
    }

    [Fact]
    public async Task Roles_without_org_settings_permission_are_forbidden()
    {
        var owner = await _api.RegisterOrganizationAsync("org4 診所", "org4_owner");
        await _api.CreateUserAsync(owner, "org4_admin", "admin");
        var admin = await _api.LoginTokenAsync("org4_admin", "Pass123!");

        var read = await _api.SendAsync(HttpMethod.Get, "/api/organization", admin, "admin");
        var write = await _api.SendAsync(HttpMethod.Put, "/api/organization", admin, "admin", Changes);

        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }
}
