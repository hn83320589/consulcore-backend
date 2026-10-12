using consulcore_backend.Data;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsulCore.Tests.Integration;

// 背景排程（套餐到期、考核開放/收回）改變了業務資料，卻因為沒有「操作者」而完全沒有留下稽核紀錄
public class SystemAuditTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<List<System.Text.Json.JsonElement>> AuditLogsAsync(string ownerToken, string resourceType, int resourceId)
    {
        var logs = await ApiTestClient.ReadItemsAsync(await _api.SendAsync(HttpMethod.Get,
            $"/api/audit-logs?resourceType={resourceType}&resourceId={resourceId}", ownerToken, "owner"));
        return logs.EnumerateArray().ToList();
    }

    [Fact]
    public async Task Package_expiry_is_audited_as_a_system_action()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "sysaudit1", expand: false);
        var clientPackageId = await clinic.PurchasePackageAsync(totalSessions: 5);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var package = await db.ClientPackages.IgnoreQueryFilters().SingleAsync(p => p.Id == clientPackageId);
        package.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        await scope.ServiceProvider.GetRequiredService<BillingService>().CheckExpiredPackagesAsync();

        var expiry = Assert.Single(await AuditLogsAsync(clinic.OwnerToken, "client_package", clientPackageId),
            l => l.GetProperty("action").GetString() == "expire");
        Assert.Equal(System.Text.Json.JsonValueKind.Null, expiry.GetProperty("userId").ValueKind);
        Assert.Equal("系統", expiry.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task Scheduled_review_visibility_change_is_audited_as_a_system_action()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "sysaudit2", expand: false);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var therapist = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == clinic.TherapistUserId);
        var review = new PerformanceReview
        {
            OrganizationId = therapist.OrganizationId, TherapistUserId = therapist.Id, ReviewerId = therapist.Id,
            Period = "2026-Q3", VisibleFrom = DateTime.UtcNow.AddDays(-1),
        };
        db.PerformanceReviews.Add(review);
        await db.SaveChangesAsync();

        await scope.ServiceProvider.GetRequiredService<PerformanceService>().CheckVisibilityScheduleAsync();

        var opened = Assert.Single(await AuditLogsAsync(clinic.OwnerToken, "performance_review", review.Id));
        Assert.Equal("visibility_open", opened.GetProperty("action").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, opened.GetProperty("userId").ValueKind);
    }
}
