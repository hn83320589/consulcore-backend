using System.Net;

namespace ConsulCore.Tests.Integration;

// P2：分頁參數原本沒有任何限制。page=0 讓 Skip 變成負數（500），pageSize 可以一次要求上百萬筆
public class PagingBoundsTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Theory]
    [InlineData("/api/audit-logs?page=0")]
    [InlineData("/api/audit-logs?pageSize=0")]
    [InlineData("/api/audit-logs?pageSize=101")]
    [InlineData("/api/billing?page=0")]
    [InlineData("/api/billing?pageSize=101")]
    public async Task Out_of_range_paging_is_rejected(string url)
    {
        var owner = await _api.RegisterOrganizationAsync("分頁診所", $"paging_{Guid.NewGuid():N}"[..20]);

        var response = await _api.SendAsync(HttpMethod.Get, url, owner, "owner");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Maximum_page_size_is_allowed()
    {
        var owner = await _api.RegisterOrganizationAsync("分頁診所", "paging_max");

        var response = await _api.SendAsync(HttpMethod.Get, "/api/audit-logs?page=1&pageSize=100", owner, "owner");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
