using System.Net;

namespace ConsulCore.Tests.Integration;

// 鎖住首次登入流程：用預設密碼登入的個案，改密碼前只能呼叫白名單端點
public class MustChangePasswordTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Client_must_change_default_password_before_using_other_endpoints()
    {
        var ownerToken = await _api.RegisterOrganizationAsync("mcp1 診所", "mcp1_owner");
        var account = await _api.CreateClientAccountAsync(ownerToken);
        var login = await _api.LoginAsync(account.Username, account.Password);
        var token = login.GetProperty("token").GetString()!;

        var blocked = await _api.SendAsync(HttpMethod.Get, "/api/notifications", token, "client");
        var me = await _api.SendAsync(HttpMethod.Get, "/api/auth/me", token, "client");

        Assert.True(login.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task After_changing_password_and_logging_in_again_the_client_is_unblocked()
    {
        var ownerToken = await _api.RegisterOrganizationAsync("mcp2 診所", "mcp2_owner");

        var token = await _api.CreateActiveClientTokenAsync(ownerToken);
        var response = await _api.SendAsync(HttpMethod.Get, "/api/notifications", token, "client");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
