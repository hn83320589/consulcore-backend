using System.Net;
using consulcore_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsulCore.Tests.Integration;

// D6 / BR-13 / BR-15：刪除使用者或個案原本是硬刪除，資料庫的串聯刪除會一併刪掉預約、晤談紀錄、收費、稽核。
// 改為軟刪除：從清單與查詢消失、不能再登入或被預約，但歷史紀錄完整保留。
public class SoftDeleteTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<HttpStatusCode> DeleteAsync(string token, string url) =>
        (await _api.SendAsync(HttpMethod.Delete, url, token, "owner")).StatusCode;

    [Fact]
    public async Task Deleted_client_disappears_from_lookup_but_keeps_appointments_and_billing()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "sdc1");
        var appointmentId = await clinic.BookAndCompleteAsync();
        await clinic.PurchasePackageAsync(totalSessions: 5); // 產生收費紀錄

        Assert.Equal(HttpStatusCode.OK, await DeleteAsync(clinic.OwnerToken, $"/api/client-profiles/{clinic.ClientProfileId}"));

        var byId = await _api.SendAsync(HttpMethod.Get, $"/api/client-profiles/{clinic.ClientProfileId}", clinic.OwnerToken, "owner");
        var list = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, "/api/client-profiles", clinic.OwnerToken, "owner"));
        var appointment = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, $"/api/appointments/{appointmentId}", clinic.OwnerToken, "owner"));

        Assert.Equal(HttpStatusCode.NotFound, byId.StatusCode);
        Assert.DoesNotContain(list.EnumerateArray(), c => c.GetProperty("id").GetInt32() == clinic.ClientProfileId);
        Assert.Equal("王小明", appointment.GetProperty("clientName").GetString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var client = await db.ClientProfiles.IgnoreQueryFilters().SingleAsync(c => c.Id == clinic.ClientProfileId);
        Assert.True(client.IsDeleted);
        Assert.NotNull(client.DeletedAt);
        Assert.True(await db.PaymentRecords.IgnoreQueryFilters().AnyAsync(p => p.ClientProfileId == clinic.ClientProfileId));
    }

    [Fact]
    public async Task Deleted_client_cannot_be_booked()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "sdc2");
        await DeleteAsync(clinic.OwnerToken, $"/api/client-profiles/{clinic.ClientProfileId}");
        var slot = (await clinic.AvailableSlotsAsync(clinic.Monday)).First();

        var book = await _api.SendAsync(HttpMethod.Post, "/api/appointments", clinic.OwnerToken, "owner", new
        {
            clientProfileId = clinic.ClientProfileId, scheduleSlotId = slot.GetProperty("id").GetInt32(),
        });

        Assert.Equal(HttpStatusCode.NotFound, book.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_client_with_an_account_disables_its_login()
    {
        var owner = await _api.RegisterOrganizationAsync("sdc3 診所", "sdc3_owner");
        var account = await _api.CreateClientAccountAsync(owner);

        await DeleteAsync(owner, $"/api/client-profiles/{account.ClientProfileId}");
        var login = await _api.SendAsync(HttpMethod.Post, "/api/auth/login", null, null,
            new { username = account.Username, password = account.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Deleted_therapist_cannot_log_in_or_use_token_but_history_remains()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "sdu1");
        var appointmentId = await clinic.BookAndCompleteAsync();
        var therapistToken = await _api.LoginTokenAsync("sdu1_ther", "Pass123!");

        Assert.Equal(HttpStatusCode.OK, await DeleteAsync(clinic.OwnerToken, $"/api/users/{clinic.TherapistUserId}"));

        var users = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, "/api/users", clinic.OwnerToken, "owner"));
        var byId = await _api.SendAsync(HttpMethod.Get, $"/api/users/{clinic.TherapistUserId}", clinic.OwnerToken, "owner");
        var login = await _api.SendAsync(HttpMethod.Post, "/api/auth/login", null, null,
            new { username = "sdu1_ther", password = "Pass123!" });
        var oldToken = await _api.SendAsync(HttpMethod.Get, "/api/auth/me", therapistToken, "therapist");
        var appointment = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, $"/api/appointments/{appointmentId}", clinic.OwnerToken, "owner"));

        Assert.DoesNotContain(users.EnumerateArray(), u => u.GetProperty("id").GetInt32() == clinic.TherapistUserId);
        Assert.Equal(HttpStatusCode.NotFound, byId.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldToken.StatusCode);
        Assert.Equal("sdu1_ther", appointment.GetProperty("therapistName").GetString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.AuditLogs.IgnoreQueryFilters().AnyAsync(a => a.UserId == clinic.TherapistUserId)
            || await db.Appointments.IgnoreQueryFilters().AnyAsync(a => a.TherapistUserId == clinic.TherapistUserId));
    }

    [Fact]
    public async Task Owner_account_cannot_be_deleted()
    {
        var owner = await _api.RegisterOrganizationAsync("sdu2 診所", "sdu2_owner");
        var me = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, "/api/auth/me", owner, "owner"));

        var delete = await _api.SendAsync(HttpMethod.Delete, $"/api/users/{me.GetProperty("id").GetInt32()}", owner, "owner");

        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
    }

    [Fact]
    public async Task Deleted_therapist_is_left_out_of_the_metrics_comparison()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "sdu3", expand: false);
        await DeleteAsync(clinic.OwnerToken, $"/api/users/{clinic.TherapistUserId}");

        var comparison = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get,
            "/api/performance-reviews/metrics-comparison?startDate=2026-01-01&endDate=2026-12-31", clinic.OwnerToken, "owner"));

        Assert.DoesNotContain(comparison.EnumerateArray(), m => m.GetProperty("therapistUserId").GetInt32() == clinic.TherapistUserId);
    }
}
