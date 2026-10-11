using System.Net;

namespace ConsulCore.Tests.Integration;

// B9（決策 D7）：心理師不再預設看全機構收費與報表；匯出（含個案姓名）要留稽核
public class BillingAccessTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Therapist_cannot_read_organization_billing_or_reports()
    {
        var owner = await _api.RegisterOrganizationAsync("ba1 診所", "ba1_owner");
        await _api.CreateUserAsync(owner, "ba1_ther", "therapist");
        var therapist = await _api.LoginTokenAsync("ba1_ther", "Pass123!");

        var pending = await _api.SendAsync(HttpMethod.Get, "/api/billing/pending", therapist, "therapist");
        var export = await _api.SendAsync(HttpMethod.Get, "/api/billing/export", therapist, "therapist");
        var report = await _api.SendAsync(HttpMethod.Get, "/api/reports/monthly-revenue?year=2026", therapist, "therapist");

        Assert.Equal(HttpStatusCode.Forbidden, pending.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, export.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, report.StatusCode);
    }

    [Theory]
    [InlineData("ba2a", "/api/billing/export", "payment_record")]
    [InlineData("ba2b", "/api/audit-logs/export", "audit_log")]
    [InlineData("ba2c", "/api/audit-logs/export?format=pdf", "audit_log")]
    public async Task Exports_are_recorded_in_the_audit_log(string prefix, string url, string resourceType)
    {
        var owner = await _api.RegisterOrganizationAsync($"{prefix} 診所", $"{prefix}_owner");

        var response = await _api.SendAsync(HttpMethod.Get, url, owner, "owner");
        var logs = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, "/api/audit-logs?action=export", owner, "owner"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entry = Assert.Single(logs.EnumerateArray());
        Assert.Equal(resourceType, entry.GetProperty("resourceType").GetString());
    }
}
