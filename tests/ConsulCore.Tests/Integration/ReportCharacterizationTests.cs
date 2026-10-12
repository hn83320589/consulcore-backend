using System.Text.Json;
using consulcore_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsulCore.Tests.Integration;

// P4 特性測試：報表與績效指標改成在資料庫端計算 / 只查需要的欄位之前，先鎖住目前的計算結果
public class ReportCharacterizationTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<JsonElement> GetAsync(string token, string role, string url) =>
        await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, url, token, role));

    [Fact]
    public async Task Therapist_metrics()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "rpt1");
        var slots = await clinic.AvailableSlotsAsync(clinic.Monday);
        var completedId = await clinic.BookAsync(slots[0].GetProperty("id").GetInt32());
        var noShowId = await clinic.BookAsync(slots[2].GetProperty("id").GetInt32());
        await _api.PostOkAsync(clinic.OwnerToken, "owner", $"/api/appointments/{completedId}/complete", new { });
        await _api.PostOkAsync(clinic.OwnerToken, "owner", $"/api/appointments/{noShowId}/no-show", new { });
        var therapist = await _api.LoginTokenAsync("rpt1_ther", "Pass123!");
        var note = await _api.PostOkAsync(therapist, "therapist", "/api/session-notes", new
        {
            clientProfileId = clinic.ClientProfileId, appointmentId = completedId, soapS = "主述",
        });
        await _api.PostOkAsync(therapist, "therapist", $"/api/session-notes/{note.GetProperty("id").GetInt32()}/lock", new { });
        // 晤談紀錄依「建立日期」篩選，區間要涵蓋今天（紀錄寫在今天）
        var range = $"startDate={DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)):yyyy-MM-dd}&endDate={clinic.Monday.AddDays(6):yyyy-MM-dd}";

        var metrics = await GetAsync(clinic.OwnerToken, "owner", $"/api/performance-reviews/metrics/{clinic.TherapistUserId}?{range}");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var noteCreatedAt = await db.SessionNotes.IgnoreQueryFilters()
            .Where(n => n.Id == note.GetProperty("id").GetInt32()).Select(n => n.CreatedAt).SingleAsync();
        var sessionStart = await db.Appointments.IgnoreQueryFilters().Where(a => a.Id == completedId)
            .Select(a => new { a.ScheduleSlot.Date, a.ScheduleSlot.StartTime }).SingleAsync();
        var expectedDelay = Math.Round((decimal)(noteCreatedAt - sessionStart.Date.ToDateTime(sessionStart.StartTime)).TotalHours, 1);

        Assert.Equal(50.0m, metrics.GetProperty("appointmentCompletionRate").GetDecimal());
        Assert.Equal(50.0m, metrics.GetProperty("noShowRate").GetDecimal());
        Assert.Equal(100.0m, metrics.GetProperty("noteLockRate").GetDecimal());
        Assert.Equal(expectedDelay, metrics.GetProperty("avgNoteDelayHours").GetDecimal());
        Assert.Equal(0m, metrics.GetProperty("clientRetentionRate").GetDecimal());
        Assert.Equal(1, metrics.GetProperty("monthlySessionCount").GetInt32());
        Assert.Equal(Math.Round(50m / 60, 4), Math.Round(metrics.GetProperty("monthlySessionHours").GetDecimal(), 4));
        Assert.Equal(0m, metrics.GetProperty("paymentCompletionRate").GetDecimal());
    }

    [Fact]
    public async Task Monthly_revenue_and_package_stats()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "rpt2", expand: false);
        await clinic.PurchasePackageAsync(totalSessions: 5);  // 9000，已付款
        var low = await clinic.PurchasePackageAsync(totalSessions: 2);  // 3600，剩 2 次 → 低剩餘
        var now = DateTime.UtcNow;

        var revenue = await GetAsync(clinic.OwnerToken, "owner", $"/api/reports/monthly-revenue?year={now.Year}&month={now.Month}");
        var stats = await GetAsync(clinic.OwnerToken, "owner", "/api/reports/package-stats");

        Assert.Equal(12600m, revenue.GetProperty("totalRevenue").GetDecimal());
        Assert.Equal(12600m, revenue.GetProperty("paidAmount").GetDecimal());
        Assert.Equal(0m, revenue.GetProperty("pendingAmount").GetDecimal());
        Assert.Equal(2, revenue.GetProperty("totalTransactions").GetInt32());
        Assert.Equal(2, stats.GetProperty("activePackages").GetInt32());
        Assert.Equal(0, stats.GetProperty("expiredPackages").GetInt32());
        Assert.Equal(12600m, stats.GetProperty("totalPackageRevenue").GetDecimal());
        var lowRemaining = Assert.Single(stats.GetProperty("lowRemaining").EnumerateArray());
        Assert.Equal(low, lowRemaining.GetProperty("clientPackageId").GetInt32());
        Assert.Equal("王小明", lowRemaining.GetProperty("clientName").GetString());
    }
}
