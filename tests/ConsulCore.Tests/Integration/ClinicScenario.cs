using System.Text.Json;

namespace ConsulCore.Tests.Integration;

/// <summary>
/// 建立一間可以預約的診所：診間、心理師（綁診間、週一到週五 09-17 班表、已展班）、個案。
/// 日期一律相對於今天的「下一個週一」，避免寫死的日期過期（BR-05 不可調整過去班表）。
/// </summary>
public record ClinicScenario(ApiTestClient Api, string OwnerToken, int TherapistUserId, int ClientProfileId, DateOnly Monday)
{
    public static async Task<ClinicScenario> CreateAsync(ApiTestClient api, string prefix)
    {
        var owner = await api.RegisterOrganizationAsync($"{prefix} 診所", $"{prefix}_owner");
        var room = await api.PostOkAsync(owner, "owner", "/api/rooms", new { name = "陽光室" });
        var therapist = await api.CreateUserAsync(owner, $"{prefix}_ther", "therapist");
        var therapistId = therapist.GetProperty("id").GetInt32();
        await api.PostOkAsync(owner, "owner", $"/api/rooms/{room.GetProperty("id").GetInt32()}/assign-therapist",
            new { therapistUserId = therapistId });
        for (var day = 1; day <= 5; day++)
        {
            await api.PostOkAsync(owner, "owner", "/api/schedule-templates", new
            {
                therapistUserId = therapistId, dayOfWeek = day, startTime = "09:00", endTime = "17:00", slotDuration = 50,
            });
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysAhead = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        var monday = today.AddDays(daysAhead == 0 ? 7 : daysAhead);
        await api.PostOkAsync(owner, "owner", "/api/schedule-slots/expand",
            new { startDate = monday.ToString("yyyy-MM-dd"), endDate = monday.AddDays(4).ToString("yyyy-MM-dd") });

        var client = await api.PostOkAsync(owner, "owner", "/api/client-profiles",
            new { name = "王小明", createAccount = false, therapistUserId = therapistId });

        return new ClinicScenario(api, owner, therapistId, client.GetProperty("id").GetInt32(), monday);
    }

    public async Task<List<JsonElement>> AvailableSlotsAsync(DateOnly date)
    {
        var d = date.ToString("yyyy-MM-dd");
        var slots = await ApiTestClient.ReadDataAsync(await Api.SendAsync(HttpMethod.Get,
            $"/api/appointments/available-slots?startDate={d}&endDate={d}&therapistUserId={TherapistUserId}",
            OwnerToken, "owner"));
        return slots.EnumerateArray().ToList();
    }

    public async Task<int> BookAsync(int slotId, int? clientProfileId = null)
    {
        var appointment = await Api.PostOkAsync(OwnerToken, "owner", "/api/appointments", new
        {
            clientProfileId = clientProfileId ?? ClientProfileId,
            scheduleSlotId = slotId,
            sessionType = "individual",
            visitType = "follow_up",
        });
        return appointment.GetProperty("id").GetInt32();
    }

    public async Task<int> BookAndCompleteAsync()
    {
        var slot = (await AvailableSlotsAsync(Monday)).First();
        var appointmentId = await BookAsync(slot.GetProperty("id").GetInt32());
        await Api.PostOkAsync(OwnerToken, "owner", $"/api/appointments/{appointmentId}/complete", new { });
        return appointmentId;
    }

    /// <summary>購買套餐，回傳 ClientPackage id。</summary>
    public async Task<int> PurchasePackageAsync(int totalSessions)
    {
        var package = await Api.PostOkAsync(OwnerToken, "owner", "/api/packages", new
        {
            name = $"{totalSessions}次套餐", sessionType = "individual", totalSessions,
            totalAmount = 1800 * totalSessions, validDays = 180,
        });
        var purchased = await Api.PostOkAsync(OwnerToken, "owner", "/api/billing/purchase-package", new
        {
            clientProfileId = ClientProfileId, packageId = package.GetProperty("id").GetInt32(), paymentMethod = "cash",
        });
        return purchased.GetProperty("id").GetInt32();
    }

    public async Task<int> SessionsRemainingAsync(int clientPackageId)
    {
        var packages = await ApiTestClient.ReadDataAsync(await Api.SendAsync(HttpMethod.Get,
            $"/api/billing/packages?clientProfileId={ClientProfileId}", OwnerToken, "owner"));
        return packages.EnumerateArray().First(p => p.GetProperty("id").GetInt32() == clientPackageId)
            .GetProperty("sessionsRemaining").GetInt32();
    }
}
