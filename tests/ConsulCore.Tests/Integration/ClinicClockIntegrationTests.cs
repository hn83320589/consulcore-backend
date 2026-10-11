using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ConsulCore.Tests.Integration;

// R2：服務改用可替換的時鐘後，BR-05（不可調整過去日期班表）可以用固定時間測試
public class ClinicClockIntegrationTests
{
    [Fact]
    public async Task Schedule_expansion_treats_dates_before_clinic_today_as_past()
    {
        // 2030-01-07 16:30 UTC = 台北 2030-01-08（週二）00:30
        using var factory = new ConsulCoreApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2030, 1, 7, 16, 30, 0, TimeSpan.Zero)))));
        var api = new ApiTestClient(factory.CreateClient());
        var clinic = await ClinicScenario.CreateAsync(api, "clock1", expand: false);

        await api.PostOkAsync(clinic.OwnerToken, "owner", "/api/schedule-slots/expand",
            new { startDate = "2030-01-07", endDate = "2030-01-09" });

        Assert.Empty(await clinic.AvailableSlotsAsync(new DateOnly(2030, 1, 7)));     // 週一：已是過去
        Assert.NotEmpty(await clinic.AvailableSlotsAsync(new DateOnly(2030, 1, 8)));  // 週二：今天
        Assert.NotEmpty(await clinic.AvailableSlotsAsync(new DateOnly(2030, 1, 9)));
    }
}
