using System.Net;

namespace ConsulCore.Tests.Integration;

// B1：表單範本（含自殺意念等高敏感題目的初診評估）只能由有 org.settings 的人修改；
// 讀取限機構人員（client.view），個案不可讀。
public class FormTemplatePermissionTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private static object NewTemplate(string name) => new
    {
        name,
        sections = new[]
        {
            new { title = "基本資料", sortOrder = 1, fields = new[] { new { label = "主訴", fieldType = "text" } } },
        },
    };

    private async Task<(string owner, string therapist, string client)> SetUpAsync(string prefix)
    {
        var owner = await _api.RegisterOrganizationAsync($"{prefix} 診所", $"{prefix}_owner");
        await _api.CreateUserAsync(owner, $"{prefix}_ther", "therapist");
        var therapist = await _api.LoginTokenAsync($"{prefix}_ther", "Pass123!");
        var client = await _api.CreateActiveClientTokenAsync(owner);
        return (owner, therapist, client);
    }

    [Fact]
    public async Task Owner_can_create_update_clone_and_delete_templates()
    {
        var (owner, _, _) = await SetUpAsync("ft1");

        var created = await _api.PostOkAsync(owner, "owner", "/api/form-templates", NewTemplate("初診評估"));
        var id = created.GetProperty("id").GetInt32();
        var update = await _api.SendAsync(HttpMethod.Put, $"/api/form-templates/{id}", owner, "owner", new { name = "初診評估 v2" });
        var clone = await _api.SendAsync(HttpMethod.Post, $"/api/form-templates/{id}/clone", owner, "owner");
        var delete = await _api.SendAsync(HttpMethod.Delete, $"/api/form-templates/{id}", owner, "owner");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, clone.StatusCode);
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    [Fact]
    public async Task Therapist_can_read_but_not_modify_templates()
    {
        var (owner, therapist, _) = await SetUpAsync("ft2");
        var id = (await _api.PostOkAsync(owner, "owner", "/api/form-templates", NewTemplate("初診評估")))
            .GetProperty("id").GetInt32();

        var list = await _api.SendAsync(HttpMethod.Get, "/api/form-templates", therapist, "therapist");
        var create = await _api.SendAsync(HttpMethod.Post, "/api/form-templates", therapist, "therapist", NewTemplate("偷建"));
        var update = await _api.SendAsync(HttpMethod.Put, $"/api/form-templates/{id}", therapist, "therapist", new { name = "竄改" });
        var clone = await _api.SendAsync(HttpMethod.Post, $"/api/form-templates/{id}/clone", therapist, "therapist");
        var delete = await _api.SendAsync(HttpMethod.Delete, $"/api/form-templates/{id}", therapist, "therapist");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, clone.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Client_cannot_read_or_modify_templates()
    {
        var (owner, _, client) = await SetUpAsync("ft3");
        var id = (await _api.PostOkAsync(owner, "owner", "/api/form-templates", NewTemplate("初診評估")))
            .GetProperty("id").GetInt32();

        var list = await _api.SendAsync(HttpMethod.Get, "/api/form-templates", client, "client");
        var byId = await _api.SendAsync(HttpMethod.Get, $"/api/form-templates/{id}", client, "client");
        var create = await _api.SendAsync(HttpMethod.Post, "/api/form-templates", client, "client", NewTemplate("偷建"));
        var delete = await _api.SendAsync(HttpMethod.Delete, $"/api/form-templates/{id}", client, "client");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byId.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }
}
