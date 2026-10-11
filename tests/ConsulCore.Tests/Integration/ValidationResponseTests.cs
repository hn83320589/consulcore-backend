using System.Net;
using System.Text.Json;

namespace ConsulCore.Tests.Integration;

// B5：模型驗證失敗原本回 ASP.NET 的 ValidationProblemDetails，不符合統一的 ApiResponse 格式
public class ValidationResponseTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Invalid_input_returns_an_api_response_with_field_errors()
    {
        var owner = await _api.RegisterOrganizationAsync("val 診所", "val_owner");

        var response = await _api.SendAsync(HttpMethod.Put, "/api/auth/change-password", owner, "owner",
            new { currentPassword = "Owner123!", newPassword = "short" });
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("message").GetString()));
        Assert.NotEmpty(body.GetProperty("errors").EnumerateArray());
    }
}
