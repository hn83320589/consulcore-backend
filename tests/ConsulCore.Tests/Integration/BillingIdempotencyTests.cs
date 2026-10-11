using System.Net;

namespace ConsulCore.Tests.Integration;

// B6：同一筆完成的預約重複觸發收費（雙擊、重試）不可重複扣套餐次數或重複開單
public class BillingIdempotencyTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Repeated_billing_for_the_same_appointment_deducts_the_package_only_once()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "bill1");
        var clientPackageId = await clinic.PurchasePackageAsync(totalSessions: 10);
        var appointmentId = await clinic.BookAndCompleteAsync();

        var first = await _api.PostOkAsync(clinic.OwnerToken, "owner", $"/api/billing/on-completed/{appointmentId}", new { });
        var second = await _api.PostOkAsync(clinic.OwnerToken, "owner", $"/api/billing/on-completed/{appointmentId}", new { });

        Assert.Equal(first.GetProperty("id").GetInt32(), second.GetProperty("id").GetInt32());
        Assert.Equal(9, await clinic.SessionsRemainingAsync(clientPackageId));
    }

    [Fact]
    public async Task Appointment_that_is_not_completed_cannot_be_billed()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "bill2");
        var clientPackageId = await clinic.PurchasePackageAsync(totalSessions: 10);
        var slot = (await clinic.AvailableSlotsAsync(clinic.Monday)).First();
        var scheduledId = await clinic.BookAsync(slot.GetProperty("id").GetInt32());

        var response = await _api.SendAsync(HttpMethod.Post, $"/api/billing/on-completed/{scheduledId}",
            clinic.OwnerToken, "owner", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(10, await clinic.SessionsRemainingAsync(clientPackageId));
    }
}
