using System.Net;

namespace ConsulCore.Tests.Integration;

// D11：個案初始密碼原本是「身分證後 4 碼 + 生日 MMDD」，知道個案基本資料就能登入。
// 改為隨機產生，只在建立時回傳一次給櫃檯交付，之後任何 API 都拿不到明文。
public class ClientAccountCredentialsTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private const string DerivedPassword = "67890615"; // 舊規則：A123456789 → 6789，1995-06-15 → 0615

    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Initial_password_is_random_and_can_be_used_for_the_first_login()
    {
        var owner = await _api.RegisterOrganizationAsync("cred1 診所", "cred1_owner");

        var first = await _api.CreateClientAccountAsync(owner);
        var second = await _api.CreateClientAccountAsync(owner);
        var login = await _api.LoginAsync(first.Username, first.Password);
        var derived = await _api.SendAsync(HttpMethod.Post, "/api/auth/login", null, null,
            new { username = first.Username, password = DerivedPassword });

        Assert.True(first.Password.Length >= 10);
        Assert.NotEqual(first.Password, second.Password);
        Assert.NotEqual(first.Username, second.Username); // 原本以秒為單位的時間戳，同一秒建立會撞名
        Assert.True(login.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, derived.StatusCode);
    }

    [Fact]
    public async Task Credentials_are_never_returned_again()
    {
        var owner = await _api.RegisterOrganizationAsync("cred2 診所", "cred2_owner");
        var account = await _api.CreateClientAccountAsync(owner);

        var byId = await (await _api.SendAsync(HttpMethod.Get, $"/api/client-profiles/{account.ClientProfileId}", owner, "owner"))
            .Content.ReadAsStringAsync();
        var list = await (await _api.SendAsync(HttpMethod.Get, "/api/client-profiles", owner, "owner"))
            .Content.ReadAsStringAsync();

        Assert.DoesNotContain("initialCredentials", byId);
        Assert.DoesNotContain(account.Password, byId);
        Assert.DoesNotContain("initialCredentials", list);
    }

    [Fact]
    public async Task Client_without_account_has_no_credentials()
    {
        var owner = await _api.RegisterOrganizationAsync("cred3 診所", "cred3_owner");

        var response = await (await _api.SendAsync(HttpMethod.Post, "/api/client-profiles", owner, "owner",
            new { name = "無帳號個案", createAccount = false })).Content.ReadAsStringAsync();

        Assert.DoesNotContain("initialCredentials", response);
    }
}
