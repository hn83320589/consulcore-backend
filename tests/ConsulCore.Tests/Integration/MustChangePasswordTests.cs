using System.Net;
using System.Text.Json;

namespace ConsulCore.Tests.Integration;

// 鎖住首次登入流程：用預設密碼登入的個案，改密碼前只能呼叫白名單端點
public class MustChangePasswordTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private const string DefaultPassword = "67890615"; // 身分證後 4 碼 + 生日 MMDD

    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<string> CreateClientAccountAsync(string prefix)
    {
        var ownerToken = await _api.RegisterOrganizationAsync($"{prefix} 診所", $"{prefix}_owner");
        await _api.PostOkAsync(ownerToken, "owner", "/api/client-profiles", new
        {
            name = "首次登入個案",
            idNumber = "A123456789",
            dateOfBirth = "1995-06-15",
            createAccount = true,
        });
        var users = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, "/api/users", ownerToken, "owner"));
        return users.EnumerateArray()
            .First(u => u.GetProperty("roles").EnumerateArray().Any(r => r.GetString() == "client"))
            .GetProperty("username").GetString()!;
    }

    [Fact]
    public async Task Client_must_change_default_password_before_using_other_endpoints()
    {
        var username = await CreateClientAccountAsync("mcp1");
        var login = await _api.LoginAsync(username, DefaultPassword);
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
        var username = await CreateClientAccountAsync("mcp2");
        var firstToken = (await _api.LoginAsync(username, DefaultPassword)).GetProperty("token").GetString()!;
        var change = await _api.SendAsync(HttpMethod.Put, "/api/auth/change-password", firstToken, "client",
            new { currentPassword = DefaultPassword, newPassword = "NewPass123!" });
        await ApiTestClient.ReadDataAsync(change);

        JsonElement relogin = await _api.LoginAsync(username, "NewPass123!");
        var token = relogin.GetProperty("token").GetString()!;
        var response = await _api.SendAsync(HttpMethod.Get, "/api/notifications", token, "client");

        Assert.False(relogin.GetProperty("mustChangePassword").GetBoolean());
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
