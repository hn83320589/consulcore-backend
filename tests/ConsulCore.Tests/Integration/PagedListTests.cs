using System.Text.Json;

namespace ConsulCore.Tests.Integration;

// P2：會無限增長的清單（稽核、收費、通知）回傳分頁物件與總筆數，前端才能顯示頁碼
public class PagedListTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<JsonElement> GetAsync(string token, string role, string url) =>
        await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, url, token, role));

    private static void AssertPage(JsonElement page, int expectedItems, int expectedTotal, int pageNumber, int pageSize)
    {
        Assert.Equal(expectedItems, page.GetProperty("items").GetArrayLength());
        Assert.Equal(expectedTotal, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(pageNumber, page.GetProperty("page").GetInt32());
        Assert.Equal(pageSize, page.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task Payment_records_are_paged_with_total_count()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "paged1", expand: false);
        for (var i = 0; i < 3; i++) await clinic.PurchasePackageAsync(totalSessions: 5);

        AssertPage(await GetAsync(clinic.OwnerToken, "owner", "/api/billing?page=2&pageSize=2"), 1, 3, 2, 2);
    }

    [Fact]
    public async Task Audit_logs_are_paged_with_total_count_matching_the_filter()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "paged2", expand: false);
        for (var i = 0; i < 3; i++) await clinic.PurchasePackageAsync(totalSessions: 5);

        AssertPage(await GetAsync(clinic.OwnerToken, "owner", "/api/audit-logs?action=purchase&pageSize=2"), 2, 3, 1, 2);
    }

    [Fact]
    public async Task Notifications_are_paged_with_total_count()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "paged3");
        var slots = await clinic.AvailableSlotsAsync(clinic.Monday);
        for (var i = 0; i < 3; i++) await clinic.BookAsync(slots[i].GetProperty("id").GetInt32()); // 每次通知心理師
        var therapist = await _api.LoginTokenAsync("paged3_ther", "Pass123!");

        var all = await GetAsync(therapist, "therapist", "/api/notifications");
        var total = all.GetProperty("totalCount").GetInt32();

        Assert.True(total >= 3);
        AssertPage(await GetAsync(therapist, "therapist", "/api/notifications?page=2&pageSize=2"), Math.Min(2, total - 2), total, 2, 2);
    }
}
