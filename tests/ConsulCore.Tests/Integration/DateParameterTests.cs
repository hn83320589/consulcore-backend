using System.Net;
using System.Text.Json;

namespace ConsulCore.Tests.Integration;

// R5：controller 原本以 DateOnly.Parse(string) 解析日期，格式錯誤丟 FormatException → 500（被當成伺服器錯誤）
public class DateParameterTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Theory]
    [InlineData("/api/appointments/available-slots?startDate=not-a-date&endDate=2030-01-07")]
    [InlineData("/api/appointments/calendar?startDate=2030-01-07&endDate=2030-13-40")]
    [InlineData("/api/appointments/available-slots?endDate=2030-01-07")]
    [InlineData("/api/performance-reviews/metrics/1?startDate=yesterday&endDate=2030-01-07")]
    public async Task Invalid_or_missing_date_is_a_400_api_response(string url)
    {
        var prefix = $"d{Guid.NewGuid():N}"[..10];
        var owner = await _api.RegisterOrganizationAsync($"{prefix} 診所", $"{prefix}_owner");

        var response = await _api.SendAsync(HttpMethod.Get, url, owner, "owner");
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(body.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task Invalid_date_in_schedule_expansion_body_is_a_400()
    {
        var owner = await _api.RegisterOrganizationAsync("date-body 診所", "datebody_owner");

        var response = await _api.SendAsync(HttpMethod.Post, "/api/schedule-slots/expand", owner, "owner",
            new { startDate = "2030/01/07", endDate = "soon" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
