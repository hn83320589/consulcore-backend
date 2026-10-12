using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ConsulCore.Tests.Integration;

// B4：登入限流 + 登入失敗留稽核
public class LoginSecurityTests
{
    [Fact]
    public async Task Login_attempts_beyond_the_limit_are_rejected_with_429()
    {
        using var factory = new ConsulCoreApiFactory().WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:Login:PermitLimit", "3"));
        var api = new ApiTestClient(factory.CreateClient());

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            var response = await api.SendAsync(HttpMethod.Post, "/api/auth/login", null, null,
                new { username = "nobody", password = "wrong" });
            statuses.Add(response.StatusCode);
            if (i == 3)
            {
                var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
                Assert.False(body.GetProperty("success").GetBoolean());
            }
        }

        Assert.Equal(
            [HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests],
            statuses);
    }

    [Fact]
    public async Task Wrong_password_for_an_existing_account_is_recorded_in_the_audit_log()
    {
        using var factory = new ConsulCoreApiFactory();
        var api = new ApiTestClient(factory.CreateClient());
        var owner = await api.RegisterOrganizationAsync("ls 診所", "ls_owner");
        await api.CreateUserAsync(owner, "ls_ther", "therapist");

        var failed = await api.SendAsync(HttpMethod.Post, "/api/auth/login", null, null,
            new { username = "ls_ther", password = "wrong-password" });
        var logs = await ApiTestClient.ReadItemsAsync(
            await api.SendAsync(HttpMethod.Get, "/api/audit-logs?action=login_failed", owner, "owner"));

        Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        var entry = Assert.Single(logs.EnumerateArray());
        Assert.Equal("user", entry.GetProperty("resourceType").GetString());
    }
}
