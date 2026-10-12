namespace ConsulCore.Tests.Integration;

// R5：NotificationService 的 DTO 對應原本寫了 3 份；整理前先鎖住清單與更新的回應內容
public class NotificationTemplateTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Updating_a_template_returns_all_fields()
    {
        var owner = await _api.RegisterOrganizationAsync("nt 診所", "nt_owner");
        var templates = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, "/api/notification-templates", owner, "owner"));
        var first = templates.EnumerateArray().First();
        var id = first.GetProperty("id").GetInt32();

        var updated = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Put,
            $"/api/notification-templates/{id}", owner, "owner", new { subject = "新的主旨" }));

        Assert.Equal(24, templates.GetArrayLength());
        Assert.Equal(id, updated.GetProperty("id").GetInt32());
        Assert.Equal("新的主旨", updated.GetProperty("subject").GetString());
        Assert.Equal(first.GetProperty("eventType").GetString(), updated.GetProperty("eventType").GetString());
        Assert.Equal(first.GetProperty("channel").GetString(), updated.GetProperty("channel").GetString());
        Assert.Equal(first.GetProperty("isRequired").GetBoolean(), updated.GetProperty("isRequired").GetBoolean());
    }

    // 初始密碼已改為隨機產生（D11），範本卻仍告訴個案「身分證後4碼+生日」
    [Fact]
    public async Task Account_created_template_does_not_describe_the_retired_password_rule()
    {
        var owner = await _api.RegisterOrganizationAsync("nt2 診所", "nt2_owner");
        var templates = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, "/api/notification-templates", owner, "owner"));

        var bodies = templates.EnumerateArray()
            .Where(t => t.GetProperty("eventType").GetString() == "account_created")
            .Select(t => t.GetProperty("bodyTemplate").GetString()!)
            .ToList();

        Assert.Equal(3, bodies.Count);
        Assert.All(bodies, b => Assert.DoesNotContain("身分證", b));
        Assert.All(bodies, b => Assert.Contains("初始密碼", b));
    }
}
