using System.Text.Json;

namespace ConsulCore.Tests.Integration;

// 預約完成率 / 未到率原本以「區間內全部預約」為分母：區間延伸到未來時（例如趨勢圖的整個月份），
// 還沒發生的已排定預約也被算進去，同一位心理師總覽 100%、趨勢同月卻只有 33%。
// 已取消的同理：提前取消不是心理師未完成，且會因查詢區間是否涵蓋取消的那天而讓兩個畫面不一致
public class PerformanceMetricsTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Scheduled_and_cancelled_appointments_do_not_count_toward_completion_or_no_show_rates()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "pm1");
        var slots = await clinic.AvailableSlotsAsync(clinic.Monday);
        var completedId = await clinic.BookAsync(slots[0].GetProperty("id").GetInt32());
        var noShowId = await clinic.BookAsync(slots[1].GetProperty("id").GetInt32());
        await clinic.BookAsync(slots[2].GetProperty("id").GetInt32());  // 仍是已排定
        await clinic.BookAsync(slots[3].GetProperty("id").GetInt32());  // 仍是已排定
        var cancelledId = await clinic.BookAsync(slots[4].GetProperty("id").GetInt32());
        await _api.PostOkAsync(clinic.OwnerToken, "owner", $"/api/appointments/{cancelledId}/cancel", new { });
        await _api.PostOkAsync(clinic.OwnerToken, "owner", $"/api/appointments/{completedId}/complete", new { });
        await _api.PostOkAsync(clinic.OwnerToken, "owner", $"/api/appointments/{noShowId}/no-show", new { });
        var range = $"startDate={clinic.Monday:yyyy-MM-dd}&endDate={clinic.Monday.AddDays(6):yyyy-MM-dd}";

        var metrics = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get,
            $"/api/performance-reviews/metrics/{clinic.TherapistUserId}?{range}", clinic.OwnerToken, "owner"));

        Assert.Equal(50.0m, metrics.GetProperty("appointmentCompletionRate").GetDecimal());
        Assert.Equal(50.0m, metrics.GetProperty("noShowRate").GetDecimal());
    }
}
