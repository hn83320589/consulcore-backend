using System.Net;
using consulcore_backend.Data;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsulCore.Tests.Integration;

public class HealthAndDeliveryTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    // 健康檢查原本只回固定字串，資料庫壞掉時仍回 healthy，監控看不出問題
    [Fact]
    public async Task Health_reports_database_status()
    {
        var health = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, "/api/health", null, null));

        Assert.Equal("healthy", health.GetProperty("status").GetString());
        Assert.Equal("ok", health.GetProperty("database").GetString());
    }

    // Email / SMS 尚未實作（#14），mock 原本把紀錄標成 sent，看起來像已寄出
    [Fact]
    public async Task Email_and_sms_mocks_are_not_marked_as_sent()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "deliver1");
        var slot = (await clinic.AvailableSlotsAsync(clinic.Monday)).First();
        await clinic.BookAsync(slot.GetProperty("id").GetInt32()); // 通知心理師（三個管道）

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logs = await db.NotificationLogs.IgnoreQueryFilters()
            .Where(n => n.RecipientId == clinic.TherapistUserId)
            .Select(n => new { n.Channel, n.Status, n.SentAt })
            .ToListAsync();

        Assert.Contains(logs, l => l.Channel == NotificationChannel.InApp && l.Status == NotificationStatus.Sent);
        var external = logs.Where(l => l.Channel != NotificationChannel.InApp).ToList();
        Assert.NotEmpty(external);
        Assert.All(external, l =>
        {
            Assert.Equal(NotificationStatus.Queued, l.Status);
            Assert.Null(l.SentAt);
        });
    }
}
