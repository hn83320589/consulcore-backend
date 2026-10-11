using consulcore_backend.Data;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsulCore.Tests.Integration;

// R8：續約提醒原本沒有去重，每天（以及每次重啟）都會對同一個常態預約再寄一次
public class RenewalReminderTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    // 通知紀錄沒有事件類型欄位；此測試期間寄給該心理師的只有續約提醒，計算總數即可
    private static async Task<int> RenewalNoticesAsync(AppDbContext db, int therapistUserId) =>
        await db.NotificationLogs.IgnoreQueryFilters().CountAsync(n => n.RecipientId == therapistUserId);

    [Fact]
    public async Task Each_series_is_reminded_only_once()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "renew1");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var org = await db.Users.IgnoreQueryFilters().Where(u => u.Id == clinic.TherapistUserId).Select(u => u.OrganizationId).SingleAsync();
        var series = new AppointmentSeries
        {
            OrganizationId = org, ClientProfileId = clinic.ClientProfileId, TherapistUserId = clinic.TherapistUserId,
            DayOfWeek = 1, StartTime = new TimeOnly(10, 0), ExpiresAt = clinic.Monday.AddDays(7),
        }; // 沒有剩餘的預約 → 需要提醒續約
        db.AppointmentSeries.Add(series);
        await db.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<AppointmentSeriesService>();
        var before = await RenewalNoticesAsync(db, clinic.TherapistUserId);

        await service.NotifyPendingRenewalsAsync();
        var afterFirst = await RenewalNoticesAsync(db, clinic.TherapistUserId);
        await service.NotifyPendingRenewalsAsync(); // 隔天 / 重啟後再跑一次
        var afterSecond = await RenewalNoticesAsync(db, clinic.TherapistUserId);

        Assert.True(afterFirst > before);
        Assert.Equal(afterFirst, afterSecond);
    }
}
