using System.Text.Json;

namespace ConsulCore.Tests.Integration;

// 稽核紀錄給機構負責人閱讀與匯出：描述原本是 "client=1"、"original=2500.0 final=2500.0" 這類開發用格式
public class AuditDescriptionTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<List<string>> DescriptionsAsync(string ownerToken)
    {
        var items = await ApiTestClient.ReadItemsAsync(
            await _api.SendAsync(HttpMethod.Get, "/api/audit-logs?pageSize=100", ownerToken, "owner"));
        return items.EnumerateArray().Select(i => i.GetProperty("description").GetString() ?? "").ToList();
    }

    [Fact]
    public async Task Descriptions_use_readable_references_and_formatted_amounts()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "adesc1");
        await _api.PostOkAsync(clinic.OwnerToken, "owner", "/api/fee-schedules",
            new { name = "初談", sessionType = "individual", visitType = "follow_up", amount = 2500 });
        var appointmentId = await clinic.BookAndCompleteAsync();
        await _api.PostOkAsync(clinic.OwnerToken, "owner", $"/api/billing/on-completed/{appointmentId}", new { });

        var descriptions = await DescriptionsAsync(clinic.OwnerToken);

        Assert.DoesNotContain(descriptions, d => System.Text.RegularExpressions.Regex.IsMatch(d, @"\b[a-zA-Z]+=\S"));
        Assert.Contains(descriptions, d => d.StartsWith("建立預約") && d.Contains($"個案 #{clinic.ClientProfileId}"));
        Assert.Contains(descriptions, d => d.StartsWith("自動產生收費") && d.Contains(" 元") && !d.Contains(".0"));
    }
}
