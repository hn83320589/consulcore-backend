using consulcore_backend.Data;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsulCore.Tests.Integration;

// BR-04：展班不覆蓋已存在時段。程式碼會先查已存在的時段再跳過，但兩個展班請求同時進來時，
// 雙方都查不到對方即將寫入的時段 → 同一心理師同一時間出現兩個可預約時段。以資料庫唯一約束兜底。
public class ScheduleSlotUniquenessTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Database_rejects_a_second_slot_for_the_same_therapist_and_start_time()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "slotuniq");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.ScheduleSlots.IgnoreQueryFilters()
            .FirstAsync(s => s.TherapistUserId == clinic.TherapistUserId);

        db.ScheduleSlots.Add(new ScheduleSlot
        {
            OrganizationId = existing.OrganizationId, TherapistUserId = existing.TherapistUserId, RoomId = existing.RoomId,
            Date = existing.Date, StartTime = existing.StartTime, EndTime = existing.EndTime,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Expanding_the_same_range_twice_skips_existing_slots()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "slotuniq2");
        var range = new { startDate = clinic.Monday.ToString("yyyy-MM-dd"), endDate = clinic.Monday.AddDays(4).ToString("yyyy-MM-dd") };

        var again = await _api.PostOkAsync(clinic.OwnerToken, "owner", "/api/schedule-slots/expand", range);

        Assert.Equal(0, again.GetProperty("createdCount").GetInt32());
    }
}
