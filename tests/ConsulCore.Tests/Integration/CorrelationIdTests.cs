namespace ConsulCore.Tests.Integration;

// 每個請求一個追蹤碼：沿用呼叫端送來的（前端或閘道），沒有就產生；回應 header 帶回，log 也記錄同一個值
public class CorrelationIdTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private const string Header = "X-Correlation-Id";

    private async Task<string?> ResponseIdAsync(string? sent)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        if (sent != null) request.Headers.TryAddWithoutValidation(Header, sent);
        var response = await factory.CreateClient().SendAsync(request);
        return response.Headers.TryGetValues(Header, out var values) ? values.Single() : null;
    }

    [Fact]
    public async Task Generates_an_id_when_none_is_sent()
    {
        Assert.False(string.IsNullOrEmpty(await ResponseIdAsync(null)));
    }

    [Fact]
    public async Task Echoes_a_valid_incoming_id()
    {
        Assert.Equal("web-1a2b3c", await ResponseIdAsync("web-1a2b3c"));
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("<script>")]
    public async Task Replaces_an_invalid_incoming_id(string sent)
    {
        var id = await ResponseIdAsync(sent);

        Assert.NotEqual(sent, id);
        Assert.False(string.IsNullOrEmpty(id));
    }

    [Fact]
    public async Task Replaces_an_overly_long_incoming_id()
    {
        var sent = new string('a', 65);

        Assert.NotEqual(sent, await ResponseIdAsync(sent));
    }
}
