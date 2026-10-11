using consulcore_backend.Services;
using Microsoft.Extensions.Configuration;

namespace ConsulCore.Tests.Unit;

// R2：「今天」原本在兩個 service 各寫一份 UtcNow.AddHours(8)，測試無法控制時間
public class ClinicClockTests
{
    private static ClinicClock Clock(DateTimeOffset utcNow, string? timeZone = null) =>
        new(new FixedTimeProvider(utcNow), new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Clinic:TimeZone"] = timeZone })
            .Build());

    [Fact]
    public void Today_follows_the_clinic_time_zone_not_utc()
    {
        var clock = Clock(new DateTimeOffset(2026, 10, 11, 16, 30, 0, TimeSpan.Zero)); // 台北 10/12 00:30

        Assert.Equal(new DateOnly(2026, 10, 12), clock.Today);
    }

    [Fact]
    public void UtcNow_comes_from_the_time_provider()
    {
        var now = new DateTimeOffset(2026, 10, 11, 3, 0, 0, TimeSpan.Zero);

        Assert.Equal(now.UtcDateTime, Clock(now).UtcNow);
    }

    [Fact]
    public void Time_zone_is_configurable()
    {
        var clock = Clock(new DateTimeOffset(2026, 10, 11, 16, 30, 0, TimeSpan.Zero), "Europe/London");

        Assert.Equal(new DateOnly(2026, 10, 11), clock.Today);
    }

    // R8：背景維護改在每天診所時間固定時刻執行（不再一啟動就跑）
    [Theory]
    [InlineData(2026, 10, 11, 18, 0, 1)]   // 台北 10/12 02:00 → 03:00 還有 1 小時
    [InlineData(2026, 10, 11, 19, 30, 23.5)] // 台北 10/12 03:30 → 下一次是隔天 03:00
    [InlineData(2026, 10, 11, 19, 0, 24)]  // 剛好 03:00 → 下一次是隔天
    public void Delay_until_the_next_daily_run_follows_the_clinic_time_zone(
        int y, int m, int d, int h, int min, double expectedHours)
    {
        var clock = Clock(new DateTimeOffset(y, m, d, h, min, 0, TimeSpan.Zero));

        Assert.Equal(TimeSpan.FromHours(expectedHours), clock.UntilNext(new TimeOnly(3, 0)));
    }
}
