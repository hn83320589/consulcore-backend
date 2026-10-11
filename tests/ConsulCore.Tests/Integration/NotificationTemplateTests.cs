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
}
