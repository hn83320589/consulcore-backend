using System.Net;

namespace ConsulCore.Tests.Integration;

// B11：帳號停用後，已發出的 token 原本在 24 小時內仍可使用
public class DeactivatedAccountTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Token_of_a_deactivated_user_is_rejected_immediately()
    {
        var owner = await _api.RegisterOrganizationAsync("deact 診所", "deact_owner");
        var therapist = await _api.CreateUserAsync(owner, "deact_ther", "therapist");
        var token = await _api.LoginTokenAsync("deact_ther", "Pass123!");
        var before = await _api.SendAsync(HttpMethod.Get, "/api/auth/me", token, "therapist");

        await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Patch,
            $"/api/users/{therapist.GetProperty("id").GetInt32()}/toggle-active", owner, "owner"));
        var me = await _api.SendAsync(HttpMethod.Get, "/api/auth/me", token, "therapist");
        var clients = await _api.SendAsync(HttpMethod.Get, "/api/client-profiles", token, "therapist");

        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, clients.StatusCode);
        Assert.Empty(await me.Content.ReadAsByteArrayAsync()); // 空內容 = 前端視為登入過期
    }
}
