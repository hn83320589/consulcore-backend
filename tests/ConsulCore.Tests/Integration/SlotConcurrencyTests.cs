using System.Net;
using consulcore_backend.Data;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsulCore.Tests.Integration;

// B7：兩人同時預約同一時段時，原本都會讀到 available 並各自寫入 → 重複預約
public class SlotConcurrencyTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Second_writer_of_a_stale_slot_status_is_rejected()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "race1");
        var slotId = (await clinic.AvailableSlotsAsync(clinic.Monday)).First().GetProperty("id").GetInt32();

        using var scopeA = factory.Services.CreateScope();
        using var scopeB = factory.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<AppDbContext>();
        var slotA = await dbA.ScheduleSlots.IgnoreQueryFilters().SingleAsync(s => s.Id == slotId);
        var slotB = await dbB.ScheduleSlots.IgnoreQueryFilters().SingleAsync(s => s.Id == slotId);

        slotA.Status = SlotStatus.Reserved;
        await dbA.SaveChangesAsync();
        slotB.Status = SlotStatus.Reserved;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }

    [Fact]
    public async Task Parallel_bookings_of_one_slot_produce_at_most_one_appointment()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "race2");
        var slotId = (await clinic.AvailableSlotsAsync(clinic.Monday)).First().GetProperty("id").GetInt32();
        var clientIds = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            var c = await _api.PostOkAsync(clinic.OwnerToken, "owner", "/api/client-profiles",
                new { name = $"個案{i}", createAccount = false });
            clientIds.Add(c.GetProperty("id").GetInt32());
        }

        var responses = await Task.WhenAll(clientIds.Select(id => _api.SendAsync(HttpMethod.Post, "/api/appointments",
            clinic.OwnerToken, "owner",
            new { clientProfileId = id, scheduleSlotId = slotId, sessionType = "individual", visitType = "follow_up" })));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.OK),
            r => Assert.True(r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest, r.StatusCode.ToString()));
    }
}
