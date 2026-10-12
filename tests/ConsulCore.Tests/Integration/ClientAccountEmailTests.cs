using System.Text.Json;

namespace ConsulCore.Tests.Integration;

// 個案沒有提供 Email 時，帳號原本會被塞入 {username}@placeholder.local 這種假地址，
// 並在個案端設定頁顯示出來；之後接上寄信也會寄到不存在的地址。改為沒有就是 null。
public class ClientAccountEmailTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Client_account_without_email_has_no_email()
    {
        var owner = await _api.RegisterOrganizationAsync("mail1 診所", "mail1_owner");

        // 同機構兩個沒有 Email 的個案帳號：唯一索引不能因為 Email 都是空的而衝突
        var first = await _api.CreateClientAccountAsync(owner);
        await _api.CreateClientAccountAsync(owner);
        var login = await _api.LoginAsync(first.Username, first.Password);
        var token = login.GetProperty("token").GetString()!;
        var me = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, "/api/auth/me", token, "client"));

        Assert.Equal(JsonValueKind.Null, login.GetProperty("email").ValueKind);
        Assert.Equal(JsonValueKind.Null, me.GetProperty("email").ValueKind);
    }

    [Fact]
    public async Task Client_account_with_email_keeps_it()
    {
        var owner = await _api.RegisterOrganizationAsync("mail2 診所", "mail2_owner");
        var client = await _api.PostOkAsync(owner, "owner", "/api/client-profiles", new
        {
            name = "有信箱個案",
            idNumber = "B123456789",
            dateOfBirth = "1990-01-01",
            email = "someone@example.com",
            createAccount = true,
        });
        var credentials = client.GetProperty("initialCredentials");

        var login = await _api.LoginAsync(
            credentials.GetProperty("username").GetString()!, credentials.GetProperty("password").GetString()!);

        Assert.Equal("someone@example.com", login.GetProperty("email").GetString());
    }
}
